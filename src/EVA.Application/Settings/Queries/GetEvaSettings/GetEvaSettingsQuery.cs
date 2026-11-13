using MediatR;

namespace EVA.Application.Settings.Queries.GetEvaSettings;

public sealed record GetEvaSettingsQuery : IRequest<EvaSettingsDto>;
