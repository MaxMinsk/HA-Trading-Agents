import { createTradingApiClient } from "@/shared/api/client";

/** Shared client instance (relative base URL -> same-origin / vite dev proxy). */
export const apiClient = createTradingApiClient();
