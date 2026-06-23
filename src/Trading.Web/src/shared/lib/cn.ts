import { clsx, type ClassValue } from "clsx";

/// Conditional className helper (clsx wrapper) used across components.
export function cn(...inputs: ClassValue[]): string {
  return clsx(inputs);
}
