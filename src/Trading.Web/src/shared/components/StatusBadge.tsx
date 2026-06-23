import type { ReactNode } from "react";
import { cn } from "@/shared/lib/cn";

export type BadgeVariant = "ok" | "warn" | "danger" | "info";

export function StatusBadge({
  variant = "info",
  children,
}: {
  variant?: BadgeVariant;
  children: ReactNode;
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium",
        variant === "ok" && "bg-emerald-100 text-emerald-700",
        variant === "warn" && "bg-amber-100 text-amber-700",
        variant === "danger" && "bg-red-100 text-red-700",
        variant === "info" && "bg-slate-100 text-slate-700",
      )}
    >
      {children}
    </span>
  );
}
