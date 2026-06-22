# Трейдер-агент на Microsoft Agent SDK (Agent Framework): разбор

> Можно ли построить наш LLM-мульти-агентный трейдер (из
> `llm_multiagent_trader.md`) на стеке Microsoft вместо TradingAgents/LangGraph
> или самописного оркестратора на Anthropic SDK.
>
> Дата: 2026-06-22

---

## 1. Сначала — что такое «Microsoft Agent SDK» (не путать)

Под этим названием скрываются разные продукты:

| Продукт | Что это | Статус (2026) |
|---|---|---|
| **Microsoft Agent Framework (MAF)** ⭐ | Опенсорс-SDK для агентов и мульти-агентных workflow, Python + .NET | **GA 1.0 (апрель 2026)** — то, что обычно имеют в виду |
| Semantic Kernel | Старый SDK для AI-приложений/агентов | Maintenance (влит в MAF) |
| AutoGen | Старый мульти-агентный фреймворк (research) | Maintenance (влит в MAF) |
| Azure AI Foundry **Agent Service** | **Managed**-сервис агентов в облаке Azure | Отдельный продукт (хостинг) |

**MAF = прямой наследник Semantic Kernel и AutoGen**, объединивший их в один
поддерживаемый SDK. Для нового проекта — берём MAF, а не SK/AutoGen.

> Дальше под «Microsoft Agent SDK» имею в виду **MAF**.

---

## 2. Главная мысль: MAF — это слой оркестрации, а не трейдер

Это ровно та же роль, что играли TradingAgents (LangGraph) или самописный
оркестратор на Anthropic SDK в `llm_multiagent_trader.md`. MAF **заменяет
оркестрацию**, но всё остальное в нашей схеме остаётся прежним:

```
┌─────────────────────────────────────────────────────────┐
│ Что даёт MAF (оркестрация агентов):                       │
│  • агенты, их роли, инструменты (function tools)          │
│  • мульти-агентные паттерны: sequential / concurrent /    │
│    handoff / group chat / Magentic-One                    │
│  • graph-workflow с типобезопасной маршрутизацией         │
│  • checkpointing/resume, streaming, human-in-the-loop     │
│  • телеметрия/трейсинг, A2A (агент↔агент, даже Py↔C#)     │
├─────────────────────────────────────────────────────────┤
│ Что MAF НЕ делает (это по-прежнему твой код):             │
│  • DATA LAYER (цены/новости/sentiment, point-in-time)     │
│  • RISK LAYER как детерминированный код + kill-switch     │
│  • исполнение ордеров на Binance                          │
│  • бэктестинг (и борьба с look-ahead)                     │
└─────────────────────────────────────────────────────────┘
```

То есть **всё, что мы наработали раньше, остаётся в силе** — меняется только
«движок», который гоняет агентов и связывает их в команду.

---

## 3. Можно ли оставить Claude? — Да

MAF **модель-агностичен**. В коробке — коннекторы к: Microsoft Foundry, Azure
OpenAI, OpenAI, **Anthropic Claude**, Amazon Bedrock, Google Gemini, Ollama.

Значит два сценария:

**A. MAF + Claude (рекомендую, если уже спроектировано под Claude).**
- Python-пакет даёт клиенты `AnthropicFoundryClient`, `AnthropicBedrockClient`,
  `AnthropicVertexClient`; в .NET — NuGet `Microsoft.Agents.AI.Anthropic`.
- ⚠️ **Важный нюанс:** Anthropic-коннектор в MAF пока **не имеет полного
  паритета** с OpenAI-коннектором. Поддерживаются **function tools**, но
  пока **нет** code interpreter, hosted/local MCP, web search, tool approval,
  file search «из коробки».
- Для нашего трейдера это в основном ОК: нам нужны **function tools** (получить
  цену/новости, поставить ордер) и структурированный вывод решения — это есть.
  Web search/«инструменты-из-коробки» мы и так реализуем сами в data layer.

**B. MAF + Azure OpenAI (GPT).**
- Полный паритет фич, глубокая интеграция с Azure (если ты уже в экосистеме
  Azure: Foundry, Key Vault, мониторинг).
- Минус для нас: мы всю аналитику и промптинг проектировали под Claude; смена
  модели = переоценка качества и промптов.

> Вывод: если хочешь именно стек Microsoft, но сохранить уже выбранную модель —
> **MAF + Claude через Anthropic-коннектор** работает. Если глубоко в Azure и
> готов на GPT — **MAF + Azure OpenAI** даёт максимум фич.

---

## 4. Как наша архитектура ложится на паттерны MAF

Наши роли (из `llm_multiagent_trader.md`) → штатные паттерны MAF:

| Наш блок | Паттерн MAF |
|---|---|
| Аналитики (tech/news/sentiment/onchain) работают независимо | **concurrent** (параллельно) |
| Дебаты «бык vs медведь» | **group chat** (агенты спорят) |
| Передача от аналитиков → трейдеру → риск-менеджеру | **handoff** / **sequential** |
| Весь конвейер с ветвлениями и проверками | **graph workflow** (типобезопасные рёбра) |
| Авто-координатор сложного процесса | **Magentic-One** (если нужно) |
| Подтверждение сделки человеком | **human-in-the-loop** (встроено) |
| Длинные прогоны/возобновление | **checkpointing / resume** |

Инструменты агентов — это **строго типизированные функции** (Python/C#),
которые ты помечаешь как tools: `get_price`, `get_news`, `get_orderbook`,
и (в проде, через риск-слой) `place_order`.

---

## 5. Язык: Python или C#? (ключевое решение)

MAF — один из немногих фреймворков с **полноценной поддержкой и Python, и
.NET/C#** с консистентным API.

| | **Python** | **C# / .NET** |
|---|---|---|
| Экосистема трейдинга/данных | ✅ богатейшая (pandas, numpy, ccxt, python-binance, statsmodels, FreqTrade) | 🟡 беднее для квант-данных и ML |
| ML/бэктест-инструменты | ✅ всё под рукой | 🟡 придётся писать больше самому |
| Уникальная сила MAF | — | ✅ MAF — фактически единственный серьёзный агент-фреймворк для .NET |
| Если ты .NET-разработчик / в Azure | 🟡 | ✅ родная среда |

**Рекомендация:** для авто-трейдера с данными/ML/бэктестом — **Python** (вся
обвязка вокруг агентов проще). C# имеет смысл, только если ты уже глубоко в
.NET/Azure и готов тащить data/бэктест-слой руками.

---

## 6. Что MAF реально даёт нашему проекту (плюсы)

- **Готовые мульти-агентные паттерны** — не пишешь оркестрацию дебатов/handoff
  с нуля.
- **Типобезопасные workflow-графы** — маршрутизация решений с проверкой на
  этапе компиляции/рантайма (меньше «потерянных» сообщений между агентами).
- **Checkpointing + resume** — длинные прогоны можно ставить на паузу/возобновлять.
- **Human-in-the-loop из коробки** — удобно для «подтверди сделку перед
  отправкой», особенно на старте.
- **Телеметрия/трейсинг** — видно, кто что решил (важно для аудита и отладки edge).
- **Один стек на Py и C#**, A2A-коммуникация между ними.
- **GA + долгосрочная поддержка Microsoft** — стабильные API.

## 7. Минусы / на что смотреть

- **Anthropic-коннектор без полного паритета фич** (нет встроенных
  code-interpreter/MCP/web-search/tool-approval). Для нас терпимо, но проверь,
  что нужные тебе фичи (в основном function tools + structured output) покрыты.
- **MAF не решает специфику трейдинга** — data layer, риск-как-код,
  Binance-исполнение, бэктест с защитой от look-ahead — всё по-прежнему на тебе.
- **C#-экосистема для квант-данных тоньше** — если выберешь .NET, готовься
  писать больше инфраструктуры.
- **Привязка к экосистеме Microsoft/Azure** — выгодна, если ты в ней; иначе это
  лишний слой.
- Все прежние оговорки в силе: **низкочастотный режим, стоимость API,
  look-ahead в бэктесте, детерминированный риск-слой** (см.
  `llm_multiagent_trader.md`).

---

## 8. MAF vs альтернативы оркестрации (сравнение)

| Критерий | **MAF** | TradingAgents (LangGraph) | Самописный на Anthropic SDK |
|---|---|---|---|
| Готовность под трейдинг | 🟡 общий фреймворк | ✅ заточен под трейдинг (референс) | 🟡 пишешь сам |
| Мульти-агентные паттерны | ✅ богатые, штатные | ✅ есть | 🟡 пишешь сам |
| Языки | Python + **C#/.NET** | Python | любой |
| Claude как модель | ✅ (коннектор, частичный паритет) | ✅ | ✅ нативно, полный паритет |
| Enterprise-обвязка (checkpoint, HITL, телеметрия) | ✅ сильная | 🟡 базовая | 🟡 сам |
| Контроль/простота | 🟡 фреймворк со своими абстракциями | 🟡 | ✅ максимум контроля |
| Лучше всего, если… | ты в Microsoft/Azure или хочешь .NET | хочешь быстрый трейдинг-референс на Python | хочешь минимум зависимостей и полный контроль Claude |

---

## 9. Скелет (Python, концептуально)

> Точные имена классов/методов сверь с актуальной докой MAF — API ниже
> показывает идею, не дословный синтаксис.

```python
# pip install agent-framework  (+ anthropic-коннектор)
# Идея: каждый агент = LLM + system prompt + типизированные tools;
# собираем их в concurrent (аналитики) -> group chat (дебаты)
# -> handoff (трейдер -> риск-менеджер) workflow.

# 1) Клиент модели — Claude
chat_client = AnthropicClient(model="claude-opus-4-8")          # из ANTHROPIC_API_KEY

# 2) Инструменты — обычные типизированные функции
def get_price(symbol: str) -> dict: ...      # из твоего data layer (point-in-time)
def get_news(symbol: str) -> list[str]: ...
def get_sentiment(symbol: str) -> dict: ...
# place_order оставляем ВНЕ агента: его дёргает детерминированный risk/exec слой

# 3) Агенты-роли
technical = chat_client.create_agent(instructions=TECH_PROMPT, tools=[get_price])
news      = chat_client.create_agent(instructions=NEWS_PROMPT, tools=[get_news])
senti     = chat_client.create_agent(instructions=SENTI_PROMPT, tools=[get_sentiment])
bull      = chat_client.create_agent(instructions=BULL_PROMPT)
bear      = chat_client.create_agent(instructions=BEAR_PROMPT)
trader    = chat_client.create_agent(instructions=TRADER_PROMPT)   # structured output: TradeDecision
risk_mgr  = chat_client.create_agent(instructions=RISK_PROMPT)     # право вето

# 4) Workflow: аналитики concurrent -> дебаты group chat -> трейдер -> риск-менеджер
#    (паттерны MAF: concurrent / group-chat / handoff / graph workflow)
workflow = (
    build_workflow()
    .concurrent([technical, news, senti])
    .group_chat([bull, bear], rounds=2)
    .sequential([trader, risk_mgr])
    .build()
)

decision = workflow.run(context=point_in_time_snapshot)   # -> TradeDecision
# дальше: deterministic risk layer -> (testnet) ордер на Binance
```

Структурированное решение (`TradeDecision` с `action/size/confidence/...`)
получаем через structured/strongly-typed output агента-трейдера — так же, как
в `llm_multiagent_trader.md`.

---

## 10. Рекомендация

- **Да, MAF — рабочий выбор** под наш мульти-агентный трейдер. Он берёт на себя
  оркестрацию (роли, дебаты, handoff, workflow, checkpoint, human-in-the-loop,
  телеметрию), а специфику трейдинга (данные, риск-код, исполнение, бэктест) ты
  делаешь сам — это не меняется ни в каком фреймворке.
- **Модель:** можно **оставить Claude** через Anthropic-коннектор (проверь, что
  хватает function tools + structured output — для нас да). Если ты глубоко в
  Azure и готов на GPT — MAF + Azure OpenAI даёт максимум фич.
- **Язык:** **Python** для трейдинга/данных/ML; **C#** — только если ты .NET и в
  Azure (MAF — почти единственный серьёзный агент-фреймворк для .NET).
- **Когда MAF оправдан:** ты в экосистеме Microsoft/Azure, хочешь enterprise-
  обвязку (трейсинг, checkpoint, HITL) и/или .NET. Если хочешь минимум
  зависимостей и максимум контроля над Claude — самописный оркестратор на
  Anthropic SDK проще; если хочешь готовый трейдинг-референс на Python —
  TradingAgents.

---

## 11. Ключевые решения для следующего шага

1. **Модель в MAF:** Claude (Anthropic-коннектор) или Azure OpenAI (GPT)?
2. **Язык:** Python (рекомендую для трейдинга) или C# (.NET/Azure)?
3. **Зачем именно MAF:** ты уже в Azure/.NET? нужна enterprise-обвязка? или это
   «потому что Microsoft» — тогда стоит сравнить с более простым самописным
   вариантом.
4. Базовые вопросы из `initial_investigate.md` §16 (горизонт, капитал,
   доступность фьючерсов) — не зависят от фреймворка, но нужны для дизайна.

> Что НЕ меняется при переходе на MAF: сначала **point-in-time data layer**,
> потом single-agent baseline → мульти-агент, строгий бэктест против look-ahead,
> детерминированный риск-слой и kill-switch, testnet → минимальный капитал.

---

## Источники (проверено 2026-06)
- Microsoft Agent Framework (репозиторий): https://github.com/microsoft/agent-framework
- MAF — обзор (Microsoft Learn): https://learn.microsoft.com/en-us/agent-framework/overview/
- MAF — workflows: https://learn.microsoft.com/en-us/agent-framework/workflows/
- MAF + Anthropic (Claude) агенты: https://learn.microsoft.com/en-us/agent-framework/agents/providers/anthropic
- MAF 1.0 GA (объединение SK + AutoGen): https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0/
- SK vs AutoGen vs MAF: https://codetocloud.io/blog/microsoft-agent-frameworks-compared
- Build custom agents with Claude in MAF (C#/Python): https://www.trimjourney.com/blog/microsoft-agent-framework-custom-agents-claude-csharp-python
