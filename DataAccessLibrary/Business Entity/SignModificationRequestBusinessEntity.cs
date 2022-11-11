﻿using BusinessObjectLibrary;
using BusinessObjectLibrary.Predefined_constants;
using DataAccessLibrary.Interfaces;
using DTOsLibrary;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLibrary.Business_Entity
{
    public class SignModificationRequestBusinessEntity
    {
        private IUnitOfWork work;
        public SignModificationRequestBusinessEntity(IUnitOfWork work)
        {
            this.work = work;
        }

        public async Task<SignModificationRequest> GetSignModificationRequestAsyncById(Guid id)
        {
            return await work.SignModificationRequests.GetAsync(id);
        }

        public async Task<IEnumerable<SignModificationRequest>>
            GetSignModificationRequestsByScribeIdAndStatusAsync(Guid scribeId, int status)
        {
            return (await work.SignModificationRequests.GetAllAsync())
                .Where(p => !p.IsDeleted && p.Status == status && p.ScribeId.Equals(scribeId));
        }

        public async Task<SignModificationRequest> AddSignModificationRequest(SignModificationRequest signModificationRequest)
        {
            signModificationRequest.Id = Guid.NewGuid();
            signModificationRequest.Status = signModificationRequest.Status == 1 ? (int)Status.Unclaimed : (int)Status.Pending;
            signModificationRequest.CreatedDate = DateTime.Now;
            signModificationRequest.IsDeleted = false;
            await work.SignModificationRequests.AddAsync(signModificationRequest);
            await work.Save();

            //Get data in return for notification adding
            signModificationRequest.Scribe = (await work.Users.GetAllAsync())
                .Where(u => u.Id == signModificationRequest.ScribeId)
                .FirstOrDefault();
            return signModificationRequest;
        }
        public async Task<SignModificationRequest> UpdateSignModificationRequest(SignModificationRequest signModificationRequest)
        {
            work.SignModificationRequests.Update(signModificationRequest);
            await work.Save();
            return signModificationRequest;
        }
        public async Task RemoveSignModificationRequest(Guid signRomId)
        {
            SignModificationRequest signModificationRequest = await work.SignModificationRequests.GetAsync(signRomId);
            if (signModificationRequest != null)
            {
                signModificationRequest.IsDeleted = true;
                work.SignModificationRequests.Update(signModificationRequest);

                if (signModificationRequest.ModifyingSignId != null)
                {
                    Sign sign = await work.Signs.GetAsync((Guid)signModificationRequest.ModifyingSignId);
                    if (sign != null)
                    {
                        sign.IsDeleted = true;
                        work.Signs.Update(sign);
                    }
                }
                else if (signModificationRequest.ModifyingGpssignId != null)
                {
                    Gpssign gpssign = await work.Gpssigns.GetAsync((Guid)signModificationRequest.ModifyingGpssignId);
                    if (gpssign != null)
                    {
                        gpssign.IsDeleted = true;
                        work.Gpssigns.Update(gpssign);
                    }
                }
            }

            await work.Save();
        }

        //--------------------------------------------------
        public async Task<SignModificationRequestDTO> GetSignRomDetail(Guid modifyingSignId)
        {
            SignModificationRequest signRom =
                (await work.SignModificationRequests.GetAllMultiIncludeAsync(
                    include: signRom => signRom
                    .Include(s => s.ModifyingSign)
                    .ThenInclude(s => s.SignCategory)
                    .Include(s => s.ModifiedSign)
                    .ThenInclude(s => s.SignCategory)
                    ))
                .Where(s => s.ModifyingSignId == modifyingSignId).FirstOrDefault();
            SignModificationRequestDTO signRomDTO = new SignModificationRequestDTO
            {
                Id = signRom.Id,
                ModifyingSignId = signRom.ModifyingSignId,
                ModifiedSignId = signRom.ModifiedSignId,
                //ModifyingGpssignId
                //ModifiedGpssignId
                UserId = signRom.UserId,
                ScribeId = signRom.ScribeId,
                AdminId = signRom.AdminId,
                OperationType = signRom.OperationType,
                ImageUrl = signRom.ImageUrl,
                Status = signRom.Status,
                DeniedReason = signRom.DeniedReason,
                CreatedDate = signRom.CreatedDate,
                IsDeleted = signRom.IsDeleted,

                Admin = signRom.Admin,
                //ModifiedGpssign
                //ModifyingGpssign
            };

            if (signRom.ModifiedSign != null)
            {
                signRomDTO.ModifiedSign = new SignDTO
                {
                    Id = signRom.ModifiedSign.Id,
                    SignCategoryId = signRom.ModifiedSign.SignCategoryId,
                    Name = signRom.ModifiedSign.Name,
                    Description = signRom.ModifiedSign.Description,
                    ImageUrl = signRom.ModifiedSign.ImageUrl,
                    Status = signRom.ModifiedSign.Status,
                    IsDeleted = signRom.ModifiedSign.IsDeleted,
                    SignCategory = signRom.ModifiedSign.SignCategory,
                };

                var tmpData1 = new List<dynamic>();
                tmpData1.Add(signRomDTO.ModifiedSign);

                //With paragraph
                var tmpData2 = from tmp1 in tmpData1
                               join signParagraph in (await work.SignParagraphs.GetAllAsync())
                               on tmp1.Id equals signParagraph.SignId

                               join paragraph in (await work.Paragraphs.GetAllAsync())
                               .Where(p => !p.IsDeleted && p.Status == (int)Status.Active)
                               on signParagraph.ParagraphId equals paragraph.Id
                               select new
                               {
                                   Id = tmp1.Id,
                                   SignCategoryId = tmp1.SignCategoryId,
                                   Name = tmp1.Name,
                                   Description = tmp1.Description,
                                   ImageUrl = tmp1.ImageUrl,
                                   Status = tmp1.Status,
                                   IsDeleted = tmp1.IsDeleted,
                                   SignCategory = tmp1.SignCategory,

                                   SignParagraphParagraphId = paragraph.Id,
                                   SignParagraphParagraphName = paragraph.Name,
                                   SignParagraphParagraphDesc = paragraph.Description,

                                   SignParagraphSectionId = paragraph.SectionId
                               };
                //With section
                var tmpData3 = from tmp2 in tmpData2
                               join section in (await work.Sections.GetAllAsync())
                                .Where(sc => !sc.IsDeleted && sc.Status == (int)Status.Active)
                                on tmp2.SignParagraphSectionId equals section.Id
                               select new
                               {
                                   Id = tmp2.Id,
                                   SignCategoryId = tmp2.SignCategoryId,
                                   Name = tmp2.Name,
                                   Description = tmp2.Description,
                                   ImageUrl = tmp2.ImageUrl,
                                   Status = tmp2.Status,
                                   IsDeleted = tmp2.IsDeleted,
                                   SignCategory = tmp2.SignCategory,

                                   SignParagraphParagraphId = tmp2.SignParagraphParagraphId,
                                   SignParagraphParagraphName = tmp2.SignParagraphParagraphName,
                                   SignParagraphParagraphDesc = tmp2.SignParagraphParagraphDesc,

                                   SignParagraphSectionId = tmp2.SignParagraphSectionId,
                                   SignParagraphSectionName = section.Name,

                                   SignParagraphStatueId = section.StatueId
                               };

                //With statue
                var tmpData4 = from tmp3 in tmpData3
                               join statue in (await work.Statues.GetAllAsync())
                                .Where(st => !st.IsDeleted && st.Status == (int)Status.Active)
                                on tmp3.SignParagraphStatueId equals statue.Id
                               select new
                               {
                                   Id = tmp3.Id,
                                   SignCategoryId = tmp3.SignCategoryId,
                                   Name = tmp3.Name,
                                   Description = tmp3.Description,
                                   ImageUrl = tmp3.ImageUrl,
                                   Status = tmp3.Status,
                                   IsDeleted = tmp3.IsDeleted,
                                   SignCategory = tmp3.SignCategory,

                                   SignParagraphParagraphId = tmp3.SignParagraphParagraphId,
                                   SignParagraphParagraphName = tmp3.SignParagraphParagraphName,
                                   SignParagraphParagraphDesc = tmp3.SignParagraphParagraphDesc,

                                   SignParagraphSectionId = tmp3.SignParagraphSectionId,
                                   SignParagraphSectionName = tmp3.SignParagraphSectionName,

                                   SignParagraphStatueId = tmp3.SignParagraphStatueId,
                                   SignParagraphStatueName = statue.Name
                               };
                List<SignParagraphDTO> signParagraphList = null;

                foreach (var sign in tmpData1)
                {
                    signParagraphList = new List<SignParagraphDTO>();
                    foreach (var data in tmpData4)
                    {
                        if (data.Id == sign.Id)
                        {
                            signParagraphList.Add(new SignParagraphDTO
                            {
                                SignParagraphParagraphId = data.SignParagraphParagraphId,
                                SignParagraphParagraphName = data.SignParagraphParagraphName,
                                SignParagraphParagraphDesc = data.SignParagraphParagraphDesc,

                                SignParagraphSectionId = data.SignParagraphSectionId,
                                SignParagraphSectionName = data.SignParagraphSectionName,

                                SignParagraphStatueId = data.SignParagraphStatueId,
                                SignParagraphStatueName = data.SignParagraphStatueName
                            });
                        }
                    }
                }
                signParagraphList.OrderBy(r => int.Parse(r.SignParagraphStatueName.Split(" ")[1]))
                        .ThenBy(r => int.Parse(r.SignParagraphSectionName.Split(" ")[1]))
                        .ThenBy(r => r.SignParagraphParagraphName).ToList();
                signRomDTO.ModifiedSign.SignParagraphs = signParagraphList;
            }

            if (signRom.ModifyingSign != null)
            {
                signRomDTO.ModifyingSign = new SignDTO
                {
                    Id = signRom.ModifyingSign.Id,
                    SignCategoryId = signRom.ModifyingSign.SignCategoryId,
                    Name = signRom.ModifyingSign.Name,
                    Description = signRom.ModifyingSign.Description,
                    ImageUrl = signRom.ModifyingSign.ImageUrl,
                    Status = signRom.ModifyingSign.Status,
                    IsDeleted = signRom.ModifyingSign.IsDeleted,
                    SignCategory = signRom.ModifyingSign.SignCategory,
                };

                var tmpData1 = new List<dynamic>();
                tmpData1.Add(signRomDTO.ModifyingSign);

                //With paragraph
                var tmpData2 = from tmp1 in tmpData1
                               join signParagraph in (await work.SignParagraphs.GetAllAsync())
                               on tmp1.Id equals signParagraph.SignId

                               join paragraph in (await work.Paragraphs.GetAllAsync())
                               .Where(p => !p.IsDeleted && p.Status == (int)Status.Active)
                               on signParagraph.ParagraphId equals paragraph.Id
                               select new
                               {
                                   Id = tmp1.Id,
                                   SignCategoryId = tmp1.SignCategoryId,
                                   Name = tmp1.Name,
                                   Description = tmp1.Description,
                                   ImageUrl = tmp1.ImageUrl,
                                   Status = tmp1.Status,
                                   IsDeleted = tmp1.IsDeleted,
                                   SignCategory = tmp1.SignCategory,

                                   SignParagraphParagraphId = paragraph.Id,
                                   SignParagraphParagraphName = paragraph.Name,
                                   SignParagraphParagraphDesc = paragraph.Description,

                                   SignParagraphSectionId = paragraph.SectionId
                               };
                //With section
                var tmpData3 = from tmp2 in tmpData2
                               join section in (await work.Sections.GetAllAsync())
                                .Where(sc => !sc.IsDeleted && sc.Status == (int)Status.Active)
                                on tmp2.SignParagraphSectionId equals section.Id
                               select new
                               {
                                   Id = tmp2.Id,
                                   SignCategoryId = tmp2.SignCategoryId,
                                   Name = tmp2.Name,
                                   Description = tmp2.Description,
                                   ImageUrl = tmp2.ImageUrl,
                                   Status = tmp2.Status,
                                   IsDeleted = tmp2.IsDeleted,
                                   SignCategory = tmp2.SignCategory,

                                   SignParagraphParagraphId = tmp2.SignParagraphParagraphId,
                                   SignParagraphParagraphName = tmp2.SignParagraphParagraphName,
                                   SignParagraphParagraphDesc = tmp2.SignParagraphParagraphDesc,

                                   SignParagraphSectionId = tmp2.SignParagraphSectionId,
                                   SignParagraphSectionName = section.Name,

                                   SignParagraphStatueId = section.StatueId
                               };

                //With statue
                var tmpData4 = from tmp3 in tmpData3
                               join statue in (await work.Statues.GetAllAsync())
                                .Where(st => !st.IsDeleted && st.Status == (int)Status.Active)
                                on tmp3.SignParagraphStatueId equals statue.Id
                               select new
                               {
                                   Id = tmp3.Id,
                                   SignCategoryId = tmp3.SignCategoryId,
                                   Name = tmp3.Name,
                                   Description = tmp3.Description,
                                   ImageUrl = tmp3.ImageUrl,
                                   Status = tmp3.Status,
                                   IsDeleted = tmp3.IsDeleted,
                                   SignCategory = tmp3.SignCategory,

                                   SignParagraphParagraphId = tmp3.SignParagraphParagraphId,
                                   SignParagraphParagraphName = tmp3.SignParagraphParagraphName,
                                   SignParagraphParagraphDesc = tmp3.SignParagraphParagraphDesc,

                                   SignParagraphSectionId = tmp3.SignParagraphSectionId,
                                   SignParagraphSectionName = tmp3.SignParagraphSectionName,

                                   SignParagraphStatueId = tmp3.SignParagraphStatueId,
                                   SignParagraphStatueName = statue.Name
                               };
                List<SignParagraphDTO> signParagraphList = null;

                foreach (var sign in tmpData1)
                {
                    signParagraphList = new List<SignParagraphDTO>();
                    foreach (var data in tmpData4)
                    {
                        if (data.Id == sign.Id)
                        {
                            signParagraphList.Add(new SignParagraphDTO
                            {
                                SignParagraphParagraphId = data.SignParagraphParagraphId,
                                SignParagraphParagraphName = data.SignParagraphParagraphName,
                                SignParagraphParagraphDesc = data.SignParagraphParagraphDesc,

                                SignParagraphSectionId = data.SignParagraphSectionId,
                                SignParagraphSectionName = data.SignParagraphSectionName,

                                SignParagraphStatueId = data.SignParagraphStatueId,
                                SignParagraphStatueName = data.SignParagraphStatueName
                            });
                        }
                    }
                }
                signParagraphList.OrderBy(r => int.Parse(r.SignParagraphStatueName.Split(" ")[1]))
                        .ThenBy(r => int.Parse(r.SignParagraphSectionName.Split(" ")[1]))
                        .ThenBy(r => r.SignParagraphParagraphName).ToList();
                signRomDTO.ModifyingSign.SignParagraphs = signParagraphList;
            }

            return signRomDTO;
        }
        //--------------------------------------------------
        public async Task<IEnumerable<SignModificationRequest>> GetGpssignRoms(Guid scribeId)
        {
            IEnumerable<SignModificationRequest> gpssignRoms =
                (await work.SignModificationRequests.GetAllMultiIncludeAsync(
                    include: gpsSign => gpsSign
                    .Include(g => g.ModifyingGpssign)
                    .ThenInclude(m => m.Sign)
                    .Include(g => g.ModifiedGpssign)
                    .ThenInclude(m => m.Sign)
                    .Include(g => g.User))
                    )
                    .Where( //Get only Pending GPS Sign rom or scribe claimed GPS Sign Rom
                        rom => !rom.IsDeleted
                        && rom.ModifyingGpssignId != null
                        && (rom.Status == (int)Status.Unclaimed || rom.ScribeId == scribeId)
                    );
            return gpssignRoms;
        }

        //--------------------------------------------------
        public async Task<IEnumerable<SignModificationRequest>> GetRetrainRoms(Guid scribeId)
        {
            IEnumerable<SignModificationRequest> retrainRoms =
                (await work.SignModificationRequests.GetAllAsync())
                    .Where( //Get only Pending retrain roms or scribe claimed retrain Roms
                        rom => !rom.IsDeleted
                        && rom.OperationType == (int)OperationType.Retrain
                        && (rom.Status == (int)Status.Unclaimed || rom.ScribeId == scribeId)
                    );
            return retrainRoms;
        }
        //--------------------------------------------------
        public async Task<SignModificationRequest> ApproveSignRom(Guid modifyingSignId)
        {
            SignModificationRequest signRom = (await work.SignModificationRequests.GetAllAsync())
                .Where(s => s.ModifyingSignId == modifyingSignId).FirstOrDefault();

            if (signRom != null)
            {
                if (signRom.Status == (int)Status.Cancelled)
                {
                    throw new Exception("Yêu cầu đã bị hủy");
                }
            }

            if (signRom != null)
            {
                Sign modifyingSign = await work.Signs.GetAsync((Guid)signRom.ModifyingSignId);
                Sign modifiedSign = null;
                if (signRom.ModifiedSignId != null)
                {
                    modifiedSign = await work.Signs.GetAsync((Guid)signRom.ModifiedSignId);
                }

                if (signRom.OperationType == (int)OperationType.Add)
                {
                    signRom.Status = (int)Status.Approved;
                    if (modifyingSign != null)
                    {
                        modifyingSign.Status = (int)Status.Active;
                    }
                }
                else if (signRom.OperationType == (int)OperationType.Update)
                {
                    signRom.Status = (int)Status.Approved;
                    if (modifyingSign != null)
                    {
                        modifyingSign.Status = (int)Status.Active;
                    }
                    if (modifiedSign != null)
                    {
                        modifiedSign.IsDeleted = true;

                        //Reference all Pending Rom of the modifiedSignId to the new modifyingSignId
                        IEnumerable<SignModificationRequest> signRomsRefModifiedSign =
                            (await work.SignModificationRequests.GetAllAsync())
                            .Where(s => s.Status == (int)Status.Pending
                                    && s.ModifiedSignId == modifiedSign.Id);
                        foreach (SignModificationRequest signMod in signRomsRefModifiedSign)
                        {
                            signMod.ModifiedSignId = modifyingSign.Id;
                        }

                        //Reference all SignParagraph of ModifiedSignId to ModifyingSignId
                        IEnumerable<SignParagraph> modifiedSignIdSignParagraphs =
                            (await work.SignParagraphs.GetAllAsync())
                            .Where(sp => !sp.IsDeleted && sp.SignId == modifiedSign.Id);

                        if (modifiedSignIdSignParagraphs != null)
                        {
                            foreach (SignParagraph signPara in modifiedSignIdSignParagraphs)
                            {
                                signPara.SignId = modifyingSign.Id;
                            }
                        }
                    }
                }
                else if (signRom.OperationType == (int)OperationType.Delete)
                {
                    signRom.Status = (int)Status.Approved;
                    if (modifyingSign != null)
                    {
                        modifyingSign.Status = (int)Status.Active;
                    }
                    if (modifiedSign != null)
                    {
                        modifiedSign.IsDeleted = true;

                        //Set status of all Pending ROM reference to the modifiedSignId to Confirmed
                        IEnumerable<SignModificationRequest> signRomsRefModifiedSign =
                            (await work.SignModificationRequests.GetAllAsync())
                            .Where(s => s.Status == (int)Status.Pending
                                    && s.ModifiedSignId == modifiedSign.Id);

                        foreach (SignModificationRequest signMod in signRomsRefModifiedSign)
                        {
                            signMod.Status = (int)Status.Confirmed;
                        }
                    }
                }
            }
            await work.Save();

            //include in return to use in notification
            if (signRom.ScribeId != null)
            {
                signRom.Scribe = (await work.Users.GetAsync((Guid)signRom.ScribeId));
            }
            if (signRom.AdminId != null)
            {
                signRom.Admin = (await work.Users.GetAsync((Guid)signRom.AdminId));
            }
            if (signRom.UserId != null)
            {
                signRom.User = (await work.Users.GetAsync((Guid)signRom.UserId));
            }
            if (signRom.ModifyingSignId != null)
            {
                signRom.ModifyingSign = (await work.Signs.GetAsync((Guid)signRom.ModifyingSignId));
            }
            if (signRom.ModifyingGpssignId != null)
            {
                signRom.ModifyingGpssign = (await work.Gpssigns.GetAsync((Guid)signRom.ModifyingGpssignId)); ;
            }
            return signRom;
        }
        //----------------------------------------------------
        public async Task<SignModificationRequest> DenySignRom(Guid modifyingSignId, string deniedReason)
        {
            SignModificationRequest signRom = (await work.SignModificationRequests.GetAllAsync())
                .Where(s => s.ModifyingSignId == modifyingSignId).FirstOrDefault();

            if (signRom != null)
            {
                if (signRom.Status == (int)Status.Cancelled)
                {
                    throw new Exception("Yêu cầu đã bị hủy");
                }
            }

            if (signRom != null)
            {
                signRom.Status = (int)Status.Denied;
                signRom.DeniedReason = deniedReason;

                //Calculate approval rate
                double approvalRate = 1 - ((double)((await work.LawModificationRequests.GetAllAsync())
                    .Where(l => l.ScribeId == signRom.ScribeId && l.Status == (int)Status.Denied).Count()
                + (await work.SignModificationRequests.GetAllAsync())
                    .Where(s => s.ScribeId == signRom.ScribeId && s.Status == (int)Status.Denied).Count()
                + (await work.QuestionModificationRequests.GetAllAsync())
                .Where(s => s.ScribeId == signRom.ScribeId && s.Status == (int)Status.Denied).Count())
                    /
                ((await work.LawModificationRequests.GetAllAsync())
                    .Where(l => l.ScribeId == signRom.ScribeId).Count()
                + (await work.SignModificationRequests.GetAllAsync())
                    .Where(s => s.ScribeId == signRom.ScribeId).Count()
                + (await work.QuestionModificationRequests.GetAllAsync())
                .Where(s => s.ScribeId == signRom.ScribeId).Count()));
                if (approvalRate < 0.65)
                {
                    User deactivatingScribe = await work.Users.GetAsync((Guid)signRom.ScribeId);
                    deactivatingScribe.Status = (int)Status.Deactivated;

                    //Remove all assigned tasks of scribe
                    IEnumerable<AssignedColumn> assignedColumns =
                        (await work.AssignedColumns.GetAllAsync())
                        .Where(l => !l.IsDeleted && l.ScribeId == deactivatingScribe.Id);

                    IEnumerable<AssignedQuestionCategory> assignedQuestionCategories =
                        (await work.AssignedQuestionCategories.GetAllAsync())
                        .Where(l => !l.IsDeleted && l.ScribeId == deactivatingScribe.Id);

                    IEnumerable<AssignedSignCategory> assignedSignCategories =
                        (await work.AssignedSignCategories.GetAllAsync())
                        .Where(l => !l.IsDeleted && l.ScribeId == deactivatingScribe.Id);

                    foreach (AssignedColumn assignedColumn in assignedColumns)
                    {
                        work.AssignedColumns.Delete(assignedColumn);
                    }

                    foreach (AssignedSignCategory assignedSignCategory in assignedSignCategories)
                    {
                        work.AssignedSignCategories.Delete(assignedSignCategory);
                    }

                    foreach (AssignedQuestionCategory assignedQuestionCategory in assignedQuestionCategories)
                    {
                        work.AssignedQuestionCategories.Delete(assignedQuestionCategory);
                    }

                    //Hard delete all Roms of scribe
                    IEnumerable<LawModificationRequest> lawRoms = (await work.LawModificationRequests.GetAllAsync())
                                .Where(rom => rom.ScribeId == deactivatingScribe.Id);
                    IEnumerable<SignModificationRequest> signRoms = (await work.SignModificationRequests.GetAllAsync())
                        .Where(rom => rom.ScribeId == deactivatingScribe.Id && rom.ModifyingSignId != null);
                    IEnumerable<QuestionModificationRequest> questionRoms = (await work.QuestionModificationRequests.GetAllAsync())
                        .Where(rom => rom.ScribeId == deactivatingScribe.Id);

                    if (lawRoms != null)
                    {
                        foreach (LawModificationRequest lawRom in lawRoms)
                        {
                            work.LawModificationRequests.Delete(lawRom);
                        }
                    }

                    if (signRoms != null)
                    {
                        foreach (SignModificationRequest sRom in signRoms)
                        {
                            work.SignModificationRequests.Delete(sRom);
                        }
                    }

                    if (questionRoms != null)
                    {
                        foreach (QuestionModificationRequest questionRom in questionRoms)
                        {
                            work.QuestionModificationRequests.Delete(questionRom);
                        }
                    }

                    //Release all GPSSign roms that are claimed by current scribe
                    IEnumerable<SignModificationRequest> claimedGpssignRoms =
                        (await work.SignModificationRequests.GetAllAsync())
                        .Where(rom => rom.ScribeId == deactivatingScribe.Id && rom.ModifyingGpssignId != null);
                    if (claimedGpssignRoms != null)
                    {
                        foreach (SignModificationRequest gpssignRom in claimedGpssignRoms)
                        {
                            gpssignRom.Status = (int)Status.Pending;
                            gpssignRom.ScribeId = null;
                        }
                    }
                }
            }

            await work.Save();

            //include in return to use in notification
            if (signRom.ScribeId != null)
            {
                signRom.Scribe = (await work.Users.GetAsync((Guid)signRom.ScribeId));
            }
            if (signRom.AdminId != null)
            {
                signRom.Admin = (await work.Users.GetAsync((Guid)signRom.AdminId));
            }
            if (signRom.UserId != null)
            {
                signRom.User = (await work.Users.GetAsync((Guid)signRom.UserId));
            }
            if (signRom.ModifyingSignId != null)
            {
                signRom.ModifyingSign = (await work.Signs.GetAsync((Guid)signRom.ModifyingSignId));
            }
            if (signRom.ModifyingGpssignId != null)
            {
                signRom.ModifyingGpssign = (await work.Gpssigns.GetAsync((Guid)signRom.ModifyingGpssignId)); ;
            }
            return signRom;
        }
        //---------------------------------------------------
        public async Task<SignModificationRequest> CancelSignRom(Guid signRomId)
        {
            SignModificationRequest signRom = (await work.SignModificationRequests.GetAsync(signRomId));

            if (signRom != null)
            {
                if (signRom.Status == (int)Status.Approved || signRom.Status == (int)Status.Denied)
                {
                    throw new Exception("Yêu cầu đã được xử lý");
                }
            }

            if (signRom != null)
            {
                signRom.Status = (int)Status.Cancelled;
            }
            await work.Save();
            return signRom;
        }
        //---------------------------------------------------
        public async Task<SignModificationRequest> ClaimGpssignRom(SignModificationRequest gpsSignRom)
        {
            SignModificationRequest rom = (await work.SignModificationRequests.GetAllAsync())
                .Where(rom => !rom.IsDeleted && rom.ModifyingGpssignId == gpsSignRom.ModifyingGpssignId)
                .FirstOrDefault();
            if (rom.ScribeId != null || rom == null)
            {
                throw new Exception("Yêu cầu không còn khả dụng");
            }
            if (rom != null)
            {
                rom.ScribeId = gpsSignRom.ScribeId;
                rom.Status = (int)Status.Claimed;
            }
            work.SignModificationRequests.Update(rom);
            await work.Save();
            return gpsSignRom;
        }
        //---------------------------------------------------
        public async Task<SignModificationRequest> ClaimRetrainRom(SignModificationRequest retrainRom)
        {
            SignModificationRequest rom = (await work.SignModificationRequests.GetAllAsync())
                .Where(rom => !rom.IsDeleted && rom.Id == retrainRom.Id)
                .FirstOrDefault();
            if (rom.ScribeId != null || rom == null)
            {
                throw new Exception("Yêu cầu không còn khả dụng");
            }
            if (rom != null)
            {
                rom.ScribeId = retrainRom.ScribeId;
                rom.Status = (int)Status.Claimed;
            }
            work.SignModificationRequests.Update(rom);
            await work.Save();
            return retrainRom;
        }
        //--------------------------------------------------
        public async Task<SignModificationRequest> ApproveGpssignRom(Guid modifyingGpssignId)
        {
            SignModificationRequest gpssignRom = (await work.SignModificationRequests.GetAllAsync())
                .Where(s => s.ModifyingGpssignId == modifyingGpssignId).FirstOrDefault();

            // All claimed GPS Sign Rom must be resolve --> can not be cancelled
            //if (gpssignRom != null)
            //{
            //    if (gpssignRom.Status == (int)Status.Cancelled)
            //    {
            //        throw new Exception("Yêu cầu đã bị hủy");
            //    }
            //}

            if (gpssignRom != null)
            {
                Gpssign modifyingGpssign = await work.Gpssigns.GetAsync((Guid)gpssignRom.ModifyingGpssignId);
                Gpssign modifiedGpssign = null;
                if (gpssignRom.ModifiedGpssignId != null)
                {
                    modifiedGpssign = await work.Gpssigns.GetAsync((Guid)gpssignRom.ModifiedGpssignId);
                }

                if (gpssignRom.OperationType == (int)OperationType.Add)
                {
                    gpssignRom.Status = (int)Status.Approved;
                    if (modifyingGpssign != null)
                    {
                        modifyingGpssign.Status = (int)Status.Active;
                    }
                }
                else if (gpssignRom.OperationType == (int)OperationType.Update)
                {
                    gpssignRom.Status = (int)Status.Approved;
                    if (modifyingGpssign != null)
                    {
                        modifyingGpssign.Status = (int)Status.Active;
                    }
                    if (modifiedGpssign != null)
                    {
                        modifiedGpssign.IsDeleted = true;

                        //Reference all Pending Rom of the modifiedGpssignId to the new modifyingGpssignId
                        IEnumerable<SignModificationRequest> gpssignRomsRefModifiedGpssign =
                            (await work.SignModificationRequests.GetAllAsync())
                            .Where(s => s.Status == (int)Status.Pending
                                    && s.ModifiedGpssignId == modifiedGpssign.Id);
                        foreach (SignModificationRequest gpssignMod in gpssignRomsRefModifiedGpssign)
                        {
                            gpssignMod.ModifiedGpssignId = modifyingGpssign.Id;
                        }
                    }
                }
                else if (gpssignRom.OperationType == (int)OperationType.Delete)
                {
                    gpssignRom.Status = (int)Status.Approved;
                    if (modifyingGpssign != null)
                    {
                        modifyingGpssign.Status = (int)Status.Active;
                    }
                    if (modifiedGpssign != null)
                    {
                        modifiedGpssign.IsDeleted = true;

                        //Set status of all Pending ROM reference to the modifiedGpssignId to Confirmed
                        IEnumerable<SignModificationRequest> gpssignRomsRefModifiedGpssign =
                            (await work.SignModificationRequests.GetAllAsync())
                            .Where(s => s.Status == (int)Status.Pending
                                    && s.ModifiedGpssignId == modifiedGpssign.Id);

                        foreach (SignModificationRequest gpssignMod in gpssignRomsRefModifiedGpssign)
                        {
                            gpssignMod.Status = (int)Status.Confirmed;
                        }
                    }
                }
            }
            await work.Save();

            //include in return to use in notification
            if (gpssignRom.ScribeId != null)
            {
                gpssignRom.Scribe = (await work.Users.GetAsync((Guid)gpssignRom.ScribeId));
            }
            if (gpssignRom.AdminId != null)
            {
                gpssignRom.Admin = (await work.Users.GetAsync((Guid)gpssignRom.AdminId));
            }
            if (gpssignRom.ModifyingGpssignId != null)
            {
                gpssignRom.ModifyingGpssign = (await work.Gpssigns.GetAsync((Guid)gpssignRom.ModifyingGpssignId));
            }

            return gpssignRom;
        }
        //----------------------------------------------------
        public async Task<SignModificationRequest> DenyGpssignRom(Guid modifyingGpssignId, string deniedReason)
        {
            SignModificationRequest gpssignRom = (await work.SignModificationRequests.GetAllAsync())
                .Where(s => s.ModifyingGpssignId == modifyingGpssignId).FirstOrDefault();


            // All claimed GPS Sign Rom must be resolve --> can not be cancelled
            //if (gpssignRom != null)
            //{
            //    if (gpssignRom.Status == (int)Status.Cancelled)
            //    {
            //        throw new Exception("Yêu cầu đã bị hủy");
            //    }
            //}

            if (gpssignRom != null)
            {
                gpssignRom.Status = (int)Status.Denied;
                gpssignRom.DeniedReason = deniedReason;

                await work.Save();

                //Calculate approval rate
                double approvalRate = 1 - ((double)((await work.LawModificationRequests.GetAllAsync())
                    .Where(l => l.ScribeId == gpssignRom.ScribeId && l.Status == (int)Status.Denied).Count()
                + (await work.SignModificationRequests.GetAllAsync())
                    .Where(s => s.ScribeId == gpssignRom.ScribeId && s.Status == (int)Status.Denied).Count()
                + (await work.QuestionModificationRequests.GetAllAsync())
                .Where(s => s.ScribeId == gpssignRom.ScribeId && s.Status == (int)Status.Denied).Count())
                    /
                ((await work.LawModificationRequests.GetAllAsync())
                    .Where(l => l.ScribeId == gpssignRom.ScribeId).Count()
                + (await work.SignModificationRequests.GetAllAsync())
                    .Where(s => s.ScribeId == gpssignRom.ScribeId).Count()
                + (await work.QuestionModificationRequests.GetAllAsync())
                .Where(s => s.ScribeId == gpssignRom.ScribeId).Count()));
                if (approvalRate < 0.65)
                {
                    User deactivatingScribe = await work.Users.GetAsync((Guid)gpssignRom.ScribeId);
                    deactivatingScribe.Status = (int)Status.Deactivated;

                    //Remove all assigned tasks of scribe
                    IEnumerable<AssignedColumn> assignedColumns =
                        (await work.AssignedColumns.GetAllAsync())
                        .Where(l => !l.IsDeleted && l.ScribeId == deactivatingScribe.Id);

                    IEnumerable<AssignedQuestionCategory> assignedQuestionCategories =
                        (await work.AssignedQuestionCategories.GetAllAsync())
                        .Where(l => !l.IsDeleted && l.ScribeId == deactivatingScribe.Id);

                    IEnumerable<AssignedSignCategory> assignedSignCategories =
                        (await work.AssignedSignCategories.GetAllAsync())
                        .Where(l => !l.IsDeleted && l.ScribeId == deactivatingScribe.Id);

                    foreach (AssignedColumn assignedColumn in assignedColumns)
                    {
                        work.AssignedColumns.Delete(assignedColumn);
                    }

                    foreach (AssignedSignCategory assignedSignCategory in assignedSignCategories)
                    {
                        work.AssignedSignCategories.Delete(assignedSignCategory);
                    }

                    foreach (AssignedQuestionCategory assignedQuestionCategory in assignedQuestionCategories)
                    {
                        work.AssignedQuestionCategories.Delete(assignedQuestionCategory);
                    }

                    //Release all GPSSign roms that are claimed by current scribe (except current GPSSign rom)
                    IEnumerable<SignModificationRequest> claimedGpssignRoms =
                        (await work.SignModificationRequests.GetAllAsync())
                        .Where(rom => rom.ScribeId == deactivatingScribe.Id && rom.ModifyingGpssignId != null
                        && rom.ModifyingGpssignId != gpssignRom.ModifyingGpssignId);
                    if (claimedGpssignRoms != null)
                    {
                        foreach (SignModificationRequest gRom in claimedGpssignRoms)
                        {
                            if (gRom.ModifyingGpssignId != gpssignRom.ModifyingGpssignId)
                            {
                                gRom.Status = (int)Status.Pending;
                                gRom.ScribeId = null;
                            }
                        }
                    }

                    //Hard delete all Roms of scribe
                    IEnumerable<LawModificationRequest> lawRoms = (await work.LawModificationRequests.GetAllAsync())
                                .Where(rom => rom.ScribeId == deactivatingScribe.Id);
                    IEnumerable<SignModificationRequest> signRoms = (await work.SignModificationRequests.GetAllAsync())
                        .Where(rom => rom.ScribeId == deactivatingScribe.Id);
                    IEnumerable<QuestionModificationRequest> questionRoms = (await work.QuestionModificationRequests.GetAllAsync())
                        .Where(rom => rom.ScribeId == deactivatingScribe.Id);

                    if (lawRoms != null)
                    {
                        foreach (LawModificationRequest lawRom in lawRoms)
                        {
                            work.LawModificationRequests.Delete(lawRom);
                        }
                    }

                    if (signRoms != null)
                    {
                        foreach (SignModificationRequest sRom in signRoms)
                        {
                            work.SignModificationRequests.Delete(sRom);
                        }
                    }

                    if (questionRoms != null)
                    {
                        foreach (QuestionModificationRequest questionRom in questionRoms)
                        {
                            work.QuestionModificationRequests.Delete(questionRom);
                        }
                    }
                }
            }

            await work.Save();

            //include in return to use in notification
            if (gpssignRom.ScribeId != null)
            {
                gpssignRom.Scribe = (await work.Users.GetAsync((Guid)gpssignRom.ScribeId));
            }
            if (gpssignRom.AdminId != null)
            {
                gpssignRom.Admin = (await work.Users.GetAsync((Guid)gpssignRom.AdminId));
            }
            if (gpssignRom.ModifyingGpssignId != null)
            {
                gpssignRom.ModifyingGpssign = (await work.Gpssigns.GetAsync((Guid)gpssignRom.ModifyingGpssignId));
            }

            return gpssignRom;
        }
    }
}
