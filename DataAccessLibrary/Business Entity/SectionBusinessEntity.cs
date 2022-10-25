﻿using BusinessObjectLibrary;
using BusinessObjectLibrary.Predefined_constants;
using DataAccessLibrary.Interfaces;
using DTOsLibrary;
using DTOsLibrary.CreateNewLaw;
using DTOsLibrary.SearchLaw;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static VNRDnTAILibrary.Utilities.StringUtils;

namespace DataAccessLibrary.Business_Entity
{
    public class SectionBusinessEntity
    {
        private IUnitOfWork work;
        public SectionBusinessEntity(IUnitOfWork work)
        {
            this.work = work;
        }
        public async Task<IEnumerable<Section>> GetSectionsAsync()
        {
            IEnumerable<Section> sections = (await work.Sections.GetAllAsync())
                .Where(section => !section.IsDeleted && section.Status == (int)Status.Active);

            List<Paragraph> paragraphs = (await work.Paragraphs.GetAllAsync())
                .Where(paragraph => !paragraph.IsDeleted
                        && paragraph.Status == (int)Status.Active).ToList();

            foreach (Section section in sections)
            {
                section.Paragraphs = paragraphs
                    .Where(paragraph => paragraph.SectionId == section.Id).ToList();
            }
            return sections;
        }

        public async Task<IEnumerable<Section>> GetSectionsByStatueIdAsync(Guid statueId)
        {
            IEnumerable<Section> sections =
                (await work.Sections.GetAllAsync(nameof(Section.VehicleCategory)))
                .Where(section => !section.IsDeleted
                        && section.Status == (int)Status.Active
                        && section.StatueId == statueId)
                .OrderBy(section => int.Parse(section.Name.Split(" ")[1]));

            List<Paragraph> paragraphs = (await work.Paragraphs.GetAllAsync())
                .Where(paragraph => !paragraph.IsDeleted
                        && paragraph.Status == (int)Status.Active).ToList();
            foreach (Section section in sections)
            {
                section.Paragraphs = paragraphs
                    .Where(paragraph => paragraph.SectionId == section.Id).ToList();
                section.VehicleCategory.Sections = null;
            }
            return sections;
        }

        public async Task<Section> GetSectionAsync(Guid id)
        {
            Section section = (await work.Sections.GetAllAsync())
                .Where(section => !section.IsDeleted && section.Id.Equals(id))
                .FirstOrDefault();

            List<Paragraph> paragraphs = (await work.Paragraphs.GetAllAsync())
                .Where(paragraph => !paragraph.IsDeleted
                        && paragraph.Status == (int)Status.Active
                        && paragraph.SectionId == section.Id).ToList();
            section.Paragraphs = paragraphs;
            return section;
        }

        public async Task<IEnumerable<SearchLawDTO>> GetSearchListByQuery(string query, string vehicleCategory)
        {

            var vehicleCategoryId = (await work.VehicleCategories.GetAllAsync()).FirstOrDefault(vc => normaliseVietnamese(vehicleCategory) == normaliseVietnamese(vc.Name)).Id;
            query = normaliseVietnamese(query);
            var res = new List<SearchLawDTO>();
            var paragraphList = (await work.Paragraphs.GetAllAsync(nameof(Paragraph.Section)))
                .Where(paragraph =>
                (normaliseVietnamese(paragraph.Description).Contains(query) || normaliseVietnamese(paragraph.Name).Contains(query))
                    && paragraph.Status == (int)Status.Active
                    && paragraph.Section.VehicleCategoryId == vehicleCategoryId
                    && !paragraph.IsDeleted)
                .ToList();


            foreach (var paragraph in paragraphList)
            {
                res.Add(new SearchLawDTO
                {
                    ParagraphDesc = paragraph.Description,
                    SectionDesc = paragraph.Section.Description,
                    MaxPenalty = paragraph.Section.MaxPenalty.ToString(),
                    MinPenalty = paragraph.Section.MinPenalty.ToString(),
                    AdditionalPenalty = paragraph.AdditionalPenalty != null ? paragraph.AdditionalPenalty.ToString() : "",
                });
            }

            var sectionsList = (await work.Sections.GetAllAsync())
            .Where(section =>
            (normaliseVietnamese(section.Description).Contains(query)
                || normaliseVietnamese(section.Name).Contains(query))
                && section.Status == (int)Status.Active
                && section.VehicleCategoryId == vehicleCategoryId
                && !section.IsDeleted)
            .ToList();

            foreach (var section in sectionsList)
            {
                res.Add(new SearchLawDTO
                {
                    ParagraphDesc = "",
                    SectionDesc = section.Description,
                    MaxPenalty = section.MaxPenalty.ToString(),
                    MinPenalty = section.MinPenalty.ToString(),
                    AdditionalPenalty = section.Paragraphs.FirstOrDefault(p => p.SectionId == section.Id).AdditionalPenalty != null ? section.Paragraphs.FirstOrDefault(p => p.SectionId == section.Id).AdditionalPenalty.ToString() : "",
                });
            }
            return res;

        }

        //This new section is created for ROM of update, delete 
        public async Task<Section> AddSectionForROM(Section section)
        {
            section.Id = Guid.NewGuid();
            section.Status = (int)Status.Deactivated;

            //If the section is for Delete ROM, then keep IsDeleted = true
            if (section.IsDeleted == true) { }
            else
            {
                section.IsDeleted = false;
            }

            await work.Sections.AddAsync(section);
            await work.Save();
            return section;
        }

        //Used for creating a new section (with/without paragraphs)
        public async Task<Section> CreateNewSection(NewSectionDTO newSectionDTO)
        {
            Section newSection = new Section
            {
                Id = Guid.NewGuid(),
                Name = newSectionDTO.Name,
                VehicleCategoryId = newSectionDTO.VehicleCategoryId,
                StatueId = newSectionDTO.StatueId,
                Description = newSectionDTO.Description,
                MinPenalty = newSectionDTO.MinPenalty,
                MaxPenalty = newSectionDTO.MaxPenalty,
                Status = (int)Status.Deactivated,
                IsDeleted = false
            };

            await work.Sections.AddAsync(newSection);

            if (newSectionDTO.IsSectionWithNoParagraph)
            {
                Paragraph newParagraph = new Paragraph
                {
                    Id = Guid.NewGuid(),
                    SectionId = newSection.Id,
                    Name = "",
                    Description = "",
                    Status = (int)Status.Deactivated,
                    AdditionalPenalty = "",
                    IsDeleted = false
                };

                await work.Paragraphs.AddAsync(newParagraph);

                if (newSectionDTO.ReferenceParagraphs != null)
                {
                    foreach (ReferenceDTO referenceParagraph in newSectionDTO.ReferenceParagraphs)
                    {
                        Reference reference = new Reference
                        {
                            ParagraphId = newParagraph.Id,
                            ReferenceParagraphId = referenceParagraph.ReferenceParagraphId,
                            IsExcluded = referenceParagraph.ReferenceParagraphIsExcluded
                        };
                        await work.References.AddAsync(reference);
                    }
                }
            }
            else
            {
                if (newSectionDTO.Paragraphs != null)
                {
                    foreach (NewParagraphDTO newParagraphDTO in newSectionDTO.Paragraphs)
                    {
                        Paragraph paragraph = new Paragraph
                        {
                            Id = Guid.NewGuid(),
                            SectionId = newSection.Id,
                            Name = newParagraphDTO.Name,
                            Description = newParagraphDTO.Description,
                            Status = (int)Status.Deactivated,
                            AdditionalPenalty = newParagraphDTO.AdditionalPenalty,
                            IsDeleted = false
                        };
                        await work.Paragraphs.AddAsync(paragraph);

                        if (newParagraphDTO.KeywordId != null)
                        {
                            KeywordParagraph keywordParagraph = new KeywordParagraph
                            {
                                KeywordId = (Guid)newParagraphDTO.KeywordId,
                                ParagraphId = paragraph.Id,
                                IsDeleted = false
                            };
                            await work.KeywordParagraphs.AddAsync(keywordParagraph);
                        }

                        if (newParagraphDTO.ReferenceParagraphs != null)
                        {
                            foreach (ReferenceDTO referenceParagraph in newParagraphDTO.ReferenceParagraphs)
                            {
                                Reference reference = new Reference
                                {
                                    ParagraphId = paragraph.Id,
                                    ReferenceParagraphId = referenceParagraph.ReferenceParagraphId,
                                    IsExcluded = referenceParagraph.ReferenceParagraphIsExcluded
                                };
                                await work.References.AddAsync(reference);
                            }
                        }
                    }
                }
            }

            await work.Save();
            return newSection;
        }

        public async Task<Section> UpdateSection(Section section)
        {
            work.Sections.Update(section);
            await work.Save();
            return section;
        }
        public async Task RemoveSection(Guid id)
        {
            Section section = await work.Sections.GetAsync(id);
            section.IsDeleted = true;
            work.Sections.Update(section);
            await work.Save();
        }
    }
}
