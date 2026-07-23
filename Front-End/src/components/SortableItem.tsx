import type { CSSProperties, ReactNode } from "react";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";

type SortableItemProps = {
  id: string;
  disabled?: boolean;
  className?: string;
  children: ReactNode;
};

/** Generic dnd-kit sortable wrapper for an arbitrary string id (a tab name, a field name...). */
export function SortableItem({ id, disabled, className, children }: SortableItemProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id,
    disabled,
  });

  const style: CSSProperties = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.6 : 1,
    zIndex: isDragging ? 20 : undefined,
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={`${className ?? ""} ${!disabled ? "cursor-grab touch-none active:cursor-grabbing" : ""}`}
      {...(!disabled ? attributes : {})}
      {...(!disabled ? listeners : {})}
    >
      {children}
    </div>
  );
}
