"use client";

import { useEffect, useId, useRef, type ReactNode } from "react";

type AdminConfirmDialogProps = {
  open: boolean;
  title: string;
  description: string;
  confirmLabel: string;
  cancelLabel?: string;
  busy?: boolean;
  tone?: "default" | "danger";
  onConfirm: () => void;
  onCancel: () => void;
};

/**
 * Accessible confirmation using native <dialog>. No dialog framework.
 */
export function AdminConfirmDialog({
  open,
  title,
  description,
  confirmLabel,
  cancelLabel = "إلغاء",
  busy = false,
  tone = "default",
  onConfirm,
  onCancel,
}: AdminConfirmDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const titleId = useId();
  const descId = useId();

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;
    if (open && !dialog.open) {
      dialog.showModal();
    } else if (!open && dialog.open) {
      dialog.close();
    }
  }, [open]);

  return (
    <dialog
      ref={dialogRef}
      className="w-[min(100%,24rem)] rounded-md border border-border bg-card p-0 text-foreground shadow-lg backdrop:bg-foreground/40"
      aria-labelledby={titleId}
      aria-describedby={descId}
      onCancel={(e) => {
        e.preventDefault();
        if (!busy) onCancel();
      }}
      onClose={() => {
        if (open && !busy) onCancel();
      }}
    >
      <div className="space-y-4 p-5">
        <div className="space-y-2">
          <h2 id={titleId} className="text-base font-semibold">
            {title}
          </h2>
          <p id={descId} className="text-sm leading-6 text-muted-foreground">
            {description}
          </p>
        </div>
        <div className="flex flex-wrap justify-end gap-2">
          <button
            type="button"
            className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted disabled:opacity-60"
            onClick={onCancel}
            disabled={busy}
          >
            {cancelLabel}
          </button>
          <button
            type="button"
            className={
              tone === "danger"
                ? "rounded-md bg-destructive px-3 py-2 text-sm font-medium text-destructive-foreground hover:opacity-95 disabled:opacity-60"
                : "rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:opacity-95 disabled:opacity-60"
            }
            onClick={onConfirm}
            disabled={busy}
          >
            {busy ? "جاري التنفيذ…" : confirmLabel}
          </button>
        </div>
      </div>
    </dialog>
  );
}

type AdminFeedbackProps = {
  tone?: "success" | "error" | "info";
  children: ReactNode;
};

export function AdminFeedback({
  tone = "info",
  children,
}: AdminFeedbackProps) {
  const role = tone === "error" ? "alert" : "status";
  const styles =
    tone === "success"
      ? "border-primary/30 bg-accent/50 text-foreground"
      : tone === "error"
        ? "border-destructive/40 bg-destructive/5 text-foreground"
        : "border-border bg-muted/40 text-foreground";

  return (
    <p
      role={role}
      className={`rounded-md border px-3 py-2 text-sm leading-6 ${styles}`}
    >
      {children}
    </p>
  );
}
