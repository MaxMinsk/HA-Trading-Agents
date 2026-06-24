import type { SettingsDto, SettingsUpdate } from "@/shared/types/api";

/** Editable form values (secrets are write-only — blank means "leave unchanged"). */
export interface SettingsForm {
  mcpUrl: string;
  llmProvider: string;
  llmModel: string;
  mcpBearer: string;
  llmApiKey: string;
}

/** Seeds the form from the (masked) DTO; secret inputs start blank. */
export function formFromDto(dto: SettingsDto): SettingsForm {
  return {
    mcpUrl: dto.mcpUrl ?? "",
    llmProvider: dto.llmProvider ?? "",
    llmModel: dto.llmModel ?? "",
    mcpBearer: "",
    llmApiKey: "",
  };
}

/** Builds the patch to POST: non-secret fields always; secret fields only when the user typed one. */
export function buildSettingsUpdate(form: SettingsForm): SettingsUpdate {
  const update: SettingsUpdate = {
    mcpUrl: form.mcpUrl,
    llmProvider: form.llmProvider,
    llmModel: form.llmModel,
  };
  if (form.mcpBearer.length > 0) {
    update.mcpBearer = form.mcpBearer;
  }
  if (form.llmApiKey.length > 0) {
    update.llmApiKey = form.llmApiKey;
  }
  return update;
}
