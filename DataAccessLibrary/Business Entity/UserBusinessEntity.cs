﻿using BusinessObjectLibrary;
using BusinessObjectLibrary.Predefined_constants;
using DataAccessLibrary.Interfaces;
using DTOsLibrary;
using DTOsLibrary.AdminReport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLibrary.Business_Entity
{
    public class UserBusinessEntity
    {
        private IUnitOfWork work;
        public UserBusinessEntity(IUnitOfWork work)
        {
            this.work = work;
        }
        public async Task<IEnumerable<User>> GetMembersAsync()
        {
            var user = (await work.Users.GetAllAsync())
                .Where(user => !user.IsDeleted && user.Role == (int)UserRoles.MEMBER);
            return user.OrderBy(u => u.Username);
        }

        public async Task<IEnumerable<User>> GetMembersByUserNameAsync(string keywordUserName)
        {
            var user = (await work.Users.GetAllAsync())
                .Where(
                    user => !user.IsDeleted &&
                    user.Role == (int)UserRoles.MEMBER &&
                    user.Username.ToLower().Contains(keywordUserName.ToLower())
                 );
            return user.OrderBy(u => u.Status).ThenBy(u => u.Username);
        }

        public async Task<IEnumerable<User>> GetScribesAsync()
        {
            IEnumerable<User> scribes = (await work.Users.GetAllAsync())
                .Where(user => !user.IsDeleted && user.Role == (int)UserRoles.SCRIBE);
            return scribes.OrderBy(u => u.Username);
        }

        public async Task<ScribeDTO> GetScribeDetail(int month, int year, Guid scribeId)
        {
            User scribe = await work.Users.GetAsync(scribeId);

            IEnumerable<LawModificationRequest> lawRoms = (await work.LawModificationRequests.GetAllAsync())
                .Where(l => !l.IsDeleted && l.ScribeId == scribeId && l.CreatedDate.Month == month && l.CreatedDate.Year == year);
            IEnumerable<SignModificationRequest> signRoms = (await work.SignModificationRequests.GetAllAsync())
                .Where(l => !l.IsDeleted && l.ScribeId == scribeId && l.CreatedDate.Month == month && l.CreatedDate.Year == year);
            IEnumerable<QuestionModificationRequest> questionRoms = (await work.QuestionModificationRequests.GetAllAsync())
                .Where(l => !l.IsDeleted && l.ScribeId == scribeId && l.CreatedDate.Month == month && l.CreatedDate.Year == year);

            ScribeDTO scribeDTO = new ScribeDTO
            {
                Id = scribe.Id,
                Username = scribe.Username,
                Password = scribe.Password,
                CreatedDate = scribe.CreatedDate,
                Status = scribe.Status,

                TotalRequestCountByMonthYear = lawRoms.Count() + signRoms.Count() + questionRoms.Count(),
                PendingRequestCountByMonthYear = lawRoms.Where(r => r.Status == (int)Status.Pending).Count()
                + signRoms.Where(r => r.Status == (int)Status.Pending).Count()
                + questionRoms.Where(r => r.Status == (int)Status.Pending).Count(),

                ApprovedRequestCountByMonthYear = lawRoms.Where(r => r.Status == (int)Status.Approved).Count()
                + signRoms.Where(r => r.Status == (int)Status.Approved).Count()
                + questionRoms.Where(r => r.Status == (int)Status.Approved).Count(),

                DeniedRequestCountByMonthYear = lawRoms.Where(r => r.Status == (int)Status.Denied).Count()
                + signRoms.Where(r => r.Status == (int)Status.Denied).Count()
                + questionRoms.Where(r => r.Status == (int)Status.Denied).Count(),

                ConfirmedRequestCountByMonthYear = lawRoms.Where(r => r.Status == (int)Status.Confirmed).Count()
                + signRoms.Where(r => r.Status == (int)Status.Confirmed).Count()
                + questionRoms.Where(r => r.Status == (int)Status.Confirmed).Count(),

                CancelledRequestCountByMonthYear = lawRoms.Where(r => r.Status == (int)Status.Cancelled).Count()
                + signRoms.Where(r => r.Status == (int)Status.Cancelled).Count()
                + questionRoms.Where(r => r.Status == (int)Status.Cancelled).Count(),
            };

            return scribeDTO;
        }

        public async Task<IEnumerable<User>> GetScribesByUserNameAsync(string keywordUserName)
        {
            var user = (await work.Users.GetAllAsync())
                .Where(
                    user => !user.IsDeleted &&
                    user.Role == (int)UserRoles.SCRIBE &&
                    user.Username.ToLower().Contains(keywordUserName.ToLower())
                 );
            return user.OrderBy(u => u.Status).ThenBy(u => u.Username);
        }

        public async Task<IEnumerable<AdminDTO>> GetAdminsAsync()
        {
            List<AdminDTO> admins =
                (from admin in (await work.Users.GetAllAsync())
                        .Where(user => !user.IsDeleted && user.Role == (int)UserRoles.ADMIN && user.Status == (int)Status.Active)
                 select new AdminDTO
                 {
                     Id = admin.Id,
                     Username = admin.Username,
                     PendingRequests = ""
                 }).ToList();

            IEnumerable<LawModificationRequest> lm =
                (await work.LawModificationRequests.GetAllAsync()).Where(lm => !lm.IsDeleted && lm.Status == (int)Status.Pending);
            IEnumerable<SignModificationRequest> sm =
                (await work.SignModificationRequests.GetAllAsync()).Where(sm => !sm.IsDeleted && sm.Status == (int)Status.Pending);
            IEnumerable<QuestionModificationRequest> qm =
                (await work.QuestionModificationRequests.GetAllAsync()).Where(qm => !qm.IsDeleted && qm.Status == (int)Status.Pending);
            IEnumerable<UserModificationRequest> um =
                (await work.UserModificationRequests.GetAllAsync()).Where(um => !um.IsDeleted && um.Status == (int)Status.Pending);
            foreach (AdminDTO admin in admins)
            {
                admin.PendingRequests = (lm.Where(lm => lm.AdminId == admin.Id).Count()
                    + sm.Where(sm => sm.AdminId == admin.Id).Count()
                    + qm.Where(qm => qm.AdminId == admin.Id).Count()
                    + um.Where(um => um.ArbitratingAdminId == admin.Id).Count()) + " yêu cầu đang chờ duyệt";
            }

            return admins.OrderBy(a => a.Username);
        }

        public async Task<User> GetUserAsync(Guid id)
        {
            return (await work.Users.GetAllAsync())
                .Where(user => !user.IsDeleted && user.Id.Equals(id))
                .FirstOrDefault();
        }
        public async Task<User> GetUserAsyncByUsername(string username)
        {
            return (await work.Users.GetAllAsync())
                .Where(user => user.Username != null
                    && (!user.IsDeleted && user.Username.Equals(username)))
                .FirstOrDefault();
        }
        public async Task<User> GetUserAsyncByGmail(string gmail)
        {
            return (await work.Users.GetAllAsync())
                .Where(user => user.Gmail != null
                    && (!user.IsDeleted && user.Status != (int)Status.Deactivated
                    && user.Gmail.Equals(gmail)))
                .FirstOrDefault();
        }

        public async Task<User> GetUserAsyncByInfo(string username, string gmail)
        {
            return (await work.Users.GetAllAsync())
                .Where(user =>
                    (user.Username != null && !user.IsDeleted
                    && user.Status != (int)Status.Deactivated && user.Username.Equals(username))
                    || (user.Gmail != null && !user.IsDeleted
                        && user.Status != (int)Status.Deactivated && user.Gmail.Equals(gmail))
                 ).FirstOrDefault();
        }

        public async Task<User> AddUser(User user)
        {
            user.Id = Guid.NewGuid();
            user.IsDeleted = false;
            await work.Users.AddAsync(user);
            await work.Save();
            return user;
        }
        public async Task<User> UpdateUser(User user)
        {
            work.Users.Update(user);
            await work.Save();
            return user;
        }

        public async Task<ScribePromotionDTO> PromoteScribe(ScribePromotionDTO scribePromotionDTO)
        {
            UserModificationRequest existedPendingUserRom =
                (await work.UserModificationRequests.GetAllAsync())
                .Where(rom => !rom.IsDeleted && rom.ModifiedUserId == scribePromotionDTO.ScribeId
                && rom.Status == (int)Status.Pending)
                .FirstOrDefault();

            ScribePromotionDTO responseScribe = null;

            if (existedPendingUserRom != null)
            {
                responseScribe = new ScribePromotionDTO
                {
                    ScribeId = scribePromotionDTO.ScribeId,
                    PromotingAdminId = scribePromotionDTO.PromotingAdminId,
                    ArbitratingAdminId = scribePromotionDTO.ArbitratingAdminId,
                    ErrorMessage = "Đã tồn tại đề xuất trở thành Quản trị viên cho nhân viên này"

                };
                return responseScribe;
            }

            User existedScribe = await work.Users.GetAsync(scribePromotionDTO.ScribeId);
            if (existedScribe.Status == (int)Status.Deactivated)
            {
                responseScribe = new ScribePromotionDTO
                {
                    ScribeId = scribePromotionDTO.ScribeId,
                    PromotingAdminId = scribePromotionDTO.PromotingAdminId,
                    ArbitratingAdminId = scribePromotionDTO.ArbitratingAdminId,
                    ErrorMessage = "Tài khoản nhân viên đã bị ngưng hoạt động bởi Quản trị viên khác"

                };
                return responseScribe;
            }

            User newScribe = null;
            if (existedScribe != null)
            {
                newScribe = new User
                {
                    Id = Guid.NewGuid(),
                    Username = existedScribe.Username,
                    Password = existedScribe.Password,
                    Role = (int)UserRoles.ADMIN,
                    Status = 6,
                    CreatedDate = DateTime.UtcNow.AddHours(7),
                    IsDeleted = false
                };

                await work.Users.AddAsync(newScribe);

                UserModificationRequest userRom = new UserModificationRequest
                {
                    ModifyingUserId = newScribe.Id,
                    ModifiedUserId = existedScribe.Id,
                    PromotingAdminId = scribePromotionDTO.PromotingAdminId,
                    ArbitratingAdminId = scribePromotionDTO.ArbitratingAdminId,
                    Status = (int)Status.Pending,
                    CreatedDate = DateTime.UtcNow.AddHours(7),
                    IsDeleted = false
                };
                await work.UserModificationRequests.AddAsync(userRom);

                scribePromotionDTO = new ScribePromotionDTO
                {
                    ScribeId = newScribe.Id,
                    ArbitratingAdminId = scribePromotionDTO.ArbitratingAdminId,
                    PromotingAdminId = scribePromotionDTO.PromotingAdminId,
                    ErrorMessage = null
                };

                //include to used in notification
                scribePromotionDTO.Scribe = (await work.Users.GetAsync(scribePromotionDTO.ScribeId));
                scribePromotionDTO.PromotingAdmin = (await work.Users.GetAsync(scribePromotionDTO.PromotingAdminId));
                scribePromotionDTO.ArbitratingAdmin = (await work.Users.GetAsync(scribePromotionDTO.ArbitratingAdminId));
            }
            await work.Save();
            return scribePromotionDTO;
        }

        //Deactivate Member: status (deactivated) + comment (isDeleted = true)
        public async Task<User> DeactivateMember(User member)
        {
            User deactivatingMember = await work.Users.GetAsync(member.Id);
            if (deactivatingMember != null)
            {
                if (deactivatingMember.Status == (int)Status.Deactivated)
                {
                    throw new Exception("Tài khoản thành viên đã bị ngưng hoạt động bởi Quản trị viên khác");
                }
            }
            deactivatingMember.Status = (int)Status.Deactivated;
            IEnumerable<Comment> comments = (await work.Comments.GetAllAsync())
                .Where(c => !c.IsDeleted && c.UserId == member.Id);
            foreach (Comment comment in comments)
            {
                comment.IsDeleted = true;
            }
            await work.Save();
            return deactivatingMember;

        }

        //Deactivate Scribe
        public async Task<User> DeactivateScribe(User scribe)
        {
            User deactivatingScribe = await work.Users.GetAsync(scribe.Id);
            if (deactivatingScribe != null)
            {
                if (deactivatingScribe.Status == (int)Status.Deactivated)
                {
                    throw new Exception("Tài khoản nhân viên đã bị ngưng hoạt động bởi Quản trị viên khác");
                }
            }
            deactivatingScribe.Status = (int)Status.Deactivated;

            //Remove all assigned tasks of scribe
            IEnumerable<AssignedColumn> assignedColumns =
                (await work.AssignedColumns.GetAllAsync())
                .Where(l => !l.IsDeleted && l.ScribeId == scribe.Id);

            IEnumerable<AssignedQuestionCategory> assignedQuestionCategories =
                (await work.AssignedQuestionCategories.GetAllAsync())
                .Where(l => !l.IsDeleted && l.ScribeId == scribe.Id);

            IEnumerable<AssignedSignCategory> assignedSignCategories =
                (await work.AssignedSignCategories.GetAllAsync())
                .Where(l => !l.IsDeleted && l.ScribeId == scribe.Id);

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

            //Hard delete all Roms of scribe
            IEnumerable<LawModificationRequest> lawRoms = (await work.LawModificationRequests.GetAllAsync())
                        .Where(rom => rom.ScribeId == scribe.Id);
            IEnumerable<SignModificationRequest> signRoms = (await work.SignModificationRequests.GetAllAsync())
                .Where(rom => rom.ScribeId == scribe.Id);
            IEnumerable<QuestionModificationRequest> questionRoms = (await work.QuestionModificationRequests.GetAllAsync())
                .Where(rom => rom.ScribeId == scribe.Id);

            if (lawRoms != null)
            {
                foreach (LawModificationRequest lawRom in lawRoms)
                {
                    work.LawModificationRequests.Delete(lawRom);
                }
            }

            if (signRoms != null)
            {
                foreach (SignModificationRequest signRom in signRoms)
                {
                    work.SignModificationRequests.Delete(signRom);
                }
            }

            if (questionRoms != null)
            {
                foreach (QuestionModificationRequest questionRom in questionRoms)
                {
                    work.QuestionModificationRequests.Delete(questionRom);
                }
            }
            await work.Save();
            return deactivatingScribe;

        }

        //Re Enable Scribe
        public async Task<User> ReEnableScribe(User scribe)
        {
            User reEnablingScribe = await work.Users.GetAsync(scribe.Id);
            if (reEnablingScribe != null)
            {
                if (reEnablingScribe.Status == (int)Status.Active)
                {
                    throw new Exception("Tài khoản nhân viên đã được kích hoạt lại bởi Quản trị viên khác");
                }
            }
            reEnablingScribe.Status = (int)Status.Active;
            await work.Save();
            return reEnablingScribe;

        }

        public async Task<User> ReEnableMember(User member)
        {
            User reEnablingMember = await work.Users.GetAsync(member.Id);
            if (reEnablingMember != null)
            {
                if (reEnablingMember.Status == (int)Status.Active)
                {
                    throw new Exception("Tài khoản thành viên đã được kích hoạt lại bởi Quản trị viên khác");
                }
            }
            reEnablingMember.Status = (int)Status.Active;
            await work.Save();
            return reEnablingMember;

        }
        public async Task<User> LoginWeb(string username, string password)
        {
            return (await work.Users.GetAllAsync())
                .Where((user) => !string.IsNullOrEmpty(user.Username) && user.Username == username
                    && !string.IsNullOrEmpty(user.Password) && user.Password == password && user.Role != (int)UserRoles.MEMBER
                    && user.Status == (int)Status.Active)
                .FirstOrDefault();
        }

        public async Task<User> LoginMobile(string username, string password)
        {
            return (await work.Users.GetAllAsync())
                .Where((user) => !user.IsDeleted && user.Username == username
                    && user.Password == password && user.Status == (int)Status.Active)
                .FirstOrDefault();
        }

        public async Task<User> RegisterMember(string username, string password, string? email)
        {
            User user = new User();
            user.Id = Guid.NewGuid();
            user.Username = !String.IsNullOrEmpty(username) ? username : email;
            user.Password = password;
            user.Gmail = email;
            user.DisplayName = !String.IsNullOrEmpty(username) ? username : email;
            user.Avatar = "https://firebasestorage.googleapis.com/v0/b/vnrdntai.appspot.com/o/images%2Favatar%2Fdefault_avatar_x025.png?alt=media";
            user.CreatedDate = DateTime.UtcNow.AddHours(7);
            user.Status = (int)Status.Active;
            user.Role = (int)UserRoles.MEMBER;
            user.IsDeleted = false;

            await work.Users.AddAsync(user);
            await work.Save();
            return user;
        }

        public async Task<User> LoginWithGmail(string gmail)
        {
            User user = (await work.Users.GetAllAsync())
                .Where((user) => !user.IsDeleted && user.Gmail != null && user.Gmail == gmail)
                .FirstOrDefault();
            if (user == null)
            {
                user = await RegisterMember(null, null, gmail);
                return user;
            }
            else if (user.Status == (int)Status.Active)
            {
                return user;
            }
            return null;
        }

        public async Task<MemberByYearReportDTO> GetMemberByYearReport()
        {
            MemberByYearReportDTO memberByYear = new MemberByYearReportDTO();
            List<string> listNumberMemberByYear = new List<string>();
            IEnumerable<User> users = await work.Users.GetAllAsync();
            int minYear = (from u in users select u.CreatedDate).Min().Year;
            for (int i = minYear; i <= DateTime.Now.Year; i++)
            {
                listNumberMemberByYear.Add(i.ToString() + "-" + users
                .Where(u => !u.IsDeleted && u.Role == (int)UserRoles.MEMBER
                        && u.CreatedDate.Year == i).Count().ToString());
            }
            memberByYear.MembersByYear = listNumberMemberByYear;
            return memberByYear;
        }

        public async Task<NewMemberReportDTO> GetNewMemberReport(int month, int year)
        {
            NewMemberReportDTO newMember = new NewMemberReportDTO();
            List<string> listNumberNewMemberByDay = new List<string>();
            IEnumerable<User> users = await work.Users.GetAllAsync();
            int dayCount = DateTime.DaysInMonth(year, month);
            for (int i = 1; i <= dayCount; i++)
            {
                listNumberNewMemberByDay.Add(i.ToString() + "-" + users
                    .Where(u => !u.IsDeleted && u.Role == (int)UserRoles.MEMBER
                            && u.CreatedDate.Day == i && u.CreatedDate.Month == month
                            && u.CreatedDate.Year == year).Count().ToString());
            }
            newMember.NewMembersByDay = listNumberNewMemberByDay;
            return newMember;
        }
        //--------------------------------------------------
        public async Task<ScribeReportDTO> GetAdminScribeReportDTO(int month, int year)
        {
            IEnumerable<User> scribes = (await work.Users.GetAllAsync())
                .Where(u => !u.IsDeleted && u.Role == (int)UserRoles.SCRIBE);

            ScribeReportDTO scribeReportDTO = new ScribeReportDTO
            {
                TotalScribeCount = scribes.Count(),
                NewScribeByMonthYearCount = scribes.Where(s => s.CreatedDate.Month == month && s.CreatedDate.Year == year).Count(),
                ActiveScribeCount = scribes.Where(s => s.Status == (int)Status.Active).Count(),
                DeactivatedScribeCount = scribes.Where(s => s.Status == (int)Status.Deactivated).Count()
            };
            return scribeReportDTO;
        }
        //--------------------------------------------------
        public async Task<ApprovalRateDTO> ScribeGetApprovalRateInformation(Guid scribeId)
        {
            return new ApprovalRateDTO
            {
                DeniedRomCount =
                    (await work.LawModificationRequests.GetAllAsync())
                    .Where(l => l.ScribeId == scribeId && l.Status == (int)Status.Denied).Count()
                    + (await work.SignModificationRequests.GetAllAsync())
                        .Where(s => s.ScribeId == scribeId && s.Status == (int)Status.Denied && s.OperationType != (int)OperationType.Retrain).Count()
                    + (await work.QuestionModificationRequests.GetAllAsync())
                    .Where(s => s.ScribeId == scribeId && s.Status == (int)Status.Denied).Count(),

                TotalRomCount =
                    (await work.LawModificationRequests.GetAllAsync())
                    .Where(l => l.ScribeId == scribeId).Count()
                    + (await work.SignModificationRequests.GetAllAsync())
                        .Where(s => s.ScribeId == scribeId && s.OperationType != (int)OperationType.Retrain).Count()
                    + (await work.QuestionModificationRequests.GetAllAsync())
                    .Where(s => s.ScribeId == scribeId).Count()
            };
        }
    }
}
