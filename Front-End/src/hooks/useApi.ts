import { useEffect, useMemo, useState } from "react";
import { apiRequest } from "../lib/api";
import type { ApiRecord } from "../lib/api";
import type { Option } from "../components/Ui";

export function useList(path: string, reloadKey = 0) {
  const [data, setData] = useState<ApiRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const controller = new AbortController();

    if (!path) {
      setData([]);
      setLoading(false);
      setError("");
      return () => controller.abort();
    }

    async function load() {
      setLoading(true);
      setError("");

      try {
        const result = await apiRequest<ApiRecord[]>(path, { signal: controller.signal });
        setData(Array.isArray(result) ? result : []);
      } catch (err) {
        if (!controller.signal.aborted) {
          setError(err instanceof Error ? err.message : "Falha ao carregar dados.");
        }
      } finally {
        if (!controller.signal.aborted) setLoading(false);
      }
    }

    load();
    return () => controller.abort();
  }, [path, reloadKey]);

  return { data, loading, error, setData };
}

export function useOptions(
  path: string,
  label: string | ((item: ApiRecord) => string),
) {
  const { data } = useList(path);

  return useMemo<Option[]>(
    () =>
      data.map((item) => ({
        value: String(item.id ?? ""),
        label: typeof label === "function" ? label(item) : String(item[label] ?? item.id ?? ""),
      })),
    [data, label],
  );
}
