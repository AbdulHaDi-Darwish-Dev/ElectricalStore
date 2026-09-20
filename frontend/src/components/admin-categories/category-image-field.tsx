"use client";

import Image from "next/image";
import { useEffect, useId, useMemo, useState } from "react";
import {
  categoryImageAcceptAttribute,
  validateCategoryImageFile,
} from "@/features/admin-categories";

type CategoryImageFieldProps = {
  currentImageUrl?: string | null;
  selectedFile: File | null;
  onFileChange: (file: File | null) => void;
  disabled?: boolean;
  error?: string | null;
  onRequestRemoveCurrent?: () => void;
  removingCurrent?: boolean;
};

export function CategoryImageField({
  currentImageUrl,
  selectedFile,
  onFileChange,
  disabled,
  error,
  onRequestRemoveCurrent,
  removingCurrent,
}: CategoryImageFieldProps) {
  const inputId = useId();
  const errorId = useId();
  const [localError, setLocalError] = useState<string | null>(null);

  const previewUrl = useMemo(() => {
    if (!selectedFile) return null;
    return URL.createObjectURL(selectedFile);
  }, [selectedFile]);

  useEffect(() => {
    if (!previewUrl) return;
    return () => {
      URL.revokeObjectURL(previewUrl);
    };
  }, [previewUrl]);

  function handleChange(fileList: FileList | null) {
    setLocalError(null);
    const file = fileList?.[0] ?? null;
    if (!file) {
      onFileChange(null);
      return;
    }
    const validation = validateCategoryImageFile(file);
    if (!validation.ok) {
      setLocalError(validation.message);
      onFileChange(null);
      return;
    }
    onFileChange(file);
  }

  const shownError = error ?? localError;

  return (
    <div className="space-y-3">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start">
        <div className="relative size-28 shrink-0 overflow-hidden rounded-md border border-border bg-muted">
          {previewUrl ? (
            // Local blob preview — next/image does not host blob: URLs.
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={previewUrl}
              alt="معاينة الصورة المختارة"
              className="size-full object-cover"
            />
          ) : currentImageUrl ? (
            <Image
              src={currentImageUrl}
              alt="صورة التصنيف الحالية"
              fill
              sizes="112px"
              className="object-cover"
            />
          ) : (
            <div className="flex size-full items-center justify-center text-xs text-muted-foreground">
              بلا صورة
            </div>
          )}
        </div>

        <div className="min-w-0 flex-1 space-y-2">
          <label htmlFor={inputId} className="text-sm font-medium">
            صورة التصنيف
          </label>
          <p className="text-xs leading-5 text-muted-foreground">
            JPEG أو PNG أو WebP · بحد أقصى 5 ميغابايت. الكتالوج العام يظهر فقط
            التصنيفات النشطة التي لديها صورة.
          </p>
          <input
            id={inputId}
            type="file"
            accept={categoryImageAcceptAttribute()}
            disabled={disabled}
            aria-invalid={shownError ? true : undefined}
            aria-describedby={shownError ? errorId : undefined}
            className="block w-full text-sm file:me-3 file:rounded-md file:border file:border-border file:bg-card file:px-3 file:py-1.5 file:text-sm"
            onChange={(e) => handleChange(e.target.files)}
          />
          {selectedFile ? (
            <button
              type="button"
              className="text-sm text-foreground underline-offset-2 hover:underline disabled:opacity-60"
              disabled={disabled}
              onClick={() => {
                setLocalError(null);
                onFileChange(null);
              }}
            >
              إلغاء الصورة المختارة
            </button>
          ) : null}
          {!selectedFile && currentImageUrl && onRequestRemoveCurrent ? (
            <button
              type="button"
              className="text-sm text-destructive underline-offset-2 hover:underline disabled:opacity-60"
              disabled={disabled || removingCurrent}
              onClick={onRequestRemoveCurrent}
            >
              {removingCurrent ? "جاري إزالة الصورة…" : "إزالة الصورة الحالية"}
            </button>
          ) : null}
        </div>
      </div>

      {shownError ? (
        <p id={errorId} className="text-sm text-destructive" role="alert">
          {shownError}
        </p>
      ) : null}
    </div>
  );
}
