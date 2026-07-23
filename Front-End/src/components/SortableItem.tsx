import type { CSSProperties, ReactNode } from "react";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";

type SortableItemProps = {
  id: string;
  disabled?: boolean;
  className?: string;
  children: ReactNode;
  /**
   * Desliga a animacao de "empurrar" os vizinhos durante o arraste. Necessario quando os itens
   * tem tamanhos bem diferentes (ex.: um campo "span: full" ao lado de campos normais numa grade),
   * caso contrario o calculo de posicao do dnd-kit erra e a reordenacao trava/buga no meio do arraste.
   */
  disableLayoutAnimation?: boolean;
};

/** Generic dnd-kit sortable wrapper for an arbitrary string id (a tab name, a field name...). */
export function SortableItem({
  id,
  disabled,
  className,
  children,
  disableLayoutAnimation,
}: SortableItemProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id,
    disabled,
    animateLayoutChanges: disableLayoutAnimation ? () => false : undefined,
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
