"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { AdminFeedback } from "@/components/admin-categories";
import {
  ROLE_LEVEL_HELP,
  adminIamKeys,
  createIamRole,
  createRoleFormSchema,
  formatPlacement,
  getIamErrorMessage,
  type CreateRoleFormValues,
  type IamRoleDto,
  type RolePlacement,
} from "@/features/admin-iam";
import { ApiError } from "@/lib/api";

type Props = {
  open: boolean;
  referenceRoles: IamRoleDto[];
  onClose: () => void;
};

const placements: RolePlacement[] = ["Above", "Below", "SameLevel"];

export function CreateRoleDialog({ open, referenceRoles, onClose }: Props) {
  const queryClient = useQueryClient();
  const form = useForm<CreateRoleFormValues>({
    resolver: zodResolver(createRoleFormSchema),
    defaultValues: {
      name: "",
      referenceRoleId: referenceRoles[0]?.id ?? "",
      placement: "Below",
    },
  });

  useEffect(() => {
    if (open) {
      form.reset({
        name: "",
        referenceRoleId: referenceRoles[0]?.id ?? "",
        placement: "Below",
      });
    }
  }, [open, referenceRoles, form]);

  const create = useMutation({
    mutationFn: (values: CreateRoleFormValues) => createIamRole(values),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: adminIamKeys.roles() });
      onClose();
    },
  });

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
      role="presentation"
      onClick={onClose}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="create-role-title"
        className="w-full max-w-md rounded-md border border-border bg-background p-4 shadow-lg"
        onClick={(e) => e.stopPropagation()}
      >
        <h2 id="create-role-title" className="text-base font-semibold">
          إنشاء دور
        </h2>
        <p className="mt-1 text-xs leading-5 text-muted-foreground">
          {ROLE_LEVEL_HELP} يُحسب المستوى تلقائياً نسبةً للدور المرجعي والموضع.
        </p>

        <form
          className="mt-4 space-y-3"
          onSubmit={form.handleSubmit((values) => create.mutate(values))}
        >
          <div>
            <label htmlFor="role-name" className="mb-1 block text-sm">
              اسم الدور
            </label>
            <input
              id="role-name"
              className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
              {...form.register("name")}
            />
            {form.formState.errors.name ? (
              <p className="mt-1 text-xs text-destructive">
                {form.formState.errors.name.message}
              </p>
            ) : null}
          </div>

          <div>
            <label htmlFor="role-ref" className="mb-1 block text-sm">
              الدور المرجعي
            </label>
            <select
              id="role-ref"
              className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
              {...form.register("referenceRoleId")}
            >
              {referenceRoles.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.name} (مستوى {r.roleLevel})
                </option>
              ))}
            </select>
            {form.formState.errors.referenceRoleId ? (
              <p className="mt-1 text-xs text-destructive">
                {form.formState.errors.referenceRoleId.message}
              </p>
            ) : null}
          </div>

          <fieldset>
            <legend className="mb-1 text-sm">الموضع النسبي</legend>
            <div className="space-y-2">
              {placements.map((p) => (
                <label key={p} className="flex items-start gap-2 text-sm">
                  <input
                    type="radio"
                    value={p}
                    {...form.register("placement")}
                    className="mt-1"
                  />
                  <span>{formatPlacement(p)}</span>
                </label>
              ))}
            </div>
          </fieldset>

          {create.isError ? (
            <AdminFeedback tone="error">{mapError(create.error)}</AdminFeedback>
          ) : null}

          <div className="flex justify-end gap-2 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
              disabled={create.isPending}
            >
              إلغاء
            </button>
            <button
              type="submit"
              disabled={create.isPending || referenceRoles.length === 0}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:opacity-95 disabled:opacity-60"
            >
              {create.isPending ? "جاري الإنشاء…" : "إنشاء"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function mapError(error: unknown): string {
  if (error instanceof ApiError) {
    return getIamErrorMessage(error.code, error.status);
  }
  return getIamErrorMessage(undefined);
}
