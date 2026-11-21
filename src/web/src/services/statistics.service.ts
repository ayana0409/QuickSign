import { dotnetHttpClient } from "@/services/http/dotnetHttpClient";
import type { DashboardOverviewStatsDto } from "@/features/statistics/types/statistics.types";

export const statisticsService = {
  getMyOverview(): Promise<DashboardOverviewStatsDto> {
    return dotnetHttpClient.get<DashboardOverviewStatsDto>("/stats/overview");
  },
};
