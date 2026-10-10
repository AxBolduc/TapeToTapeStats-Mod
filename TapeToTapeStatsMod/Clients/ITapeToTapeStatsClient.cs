using TapeToTapeMods.Domain.DTOs;

namespace TapeToTapeMods.Clients;

public interface ITapeToTapeStatsClient
{
    Task<RunReportResultDto> ReportRun(RunInfoDto runInfo);
    Task<List<RunInfoDto>> GetAllRuns();
}