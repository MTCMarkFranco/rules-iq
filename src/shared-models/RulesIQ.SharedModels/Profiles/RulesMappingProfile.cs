using AutoMapper;
using RulesIQ.SharedModels.Models;

namespace RulesIQ.SharedModels.Profiles;

/// <summary>
/// AutoMapper profile for mapping candidate rules to normalized rules.
/// </summary>
public sealed class RulesMappingProfile : Profile
{
    public RulesMappingProfile()
    {
        CreateMap<CandidateRule, NormalizedRule>()
            .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => new NormalizedRuleMetadata
            {
                SourceDocuments = new List<SourceDocumentMetadata>
                {
                    new SourceDocumentMetadata
                    {
                        SourceDocumentId = src.Metadata.SourceDocumentId,
                        SourceUri = src.Metadata.SourceUri,
                        SourceDocumentVersion = src.Metadata.SourceDocumentVersion,
                        PageNumber = src.Metadata.PageNumber,
                        CharRange = src.Metadata.CharRange
                    }
                }
            }))
            .ForMember(dest => dest.LocalParams, opt => opt.MapFrom(_ => new List<object>()))
            .ForMember(dest => dest.Actions, opt => opt.MapFrom(_ => new List<object>()));
    }
}
