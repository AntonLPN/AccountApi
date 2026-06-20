using Account.Domain.DTOs;
using Account.Domain.Entities;
using AutoMapper;

namespace Account.Domain.Mappings;

public class LoginAuditMapper : Profile
{
   public LoginAuditMapper()
   {
      CreateMap<CreateLoginAuditDto, LoginAudit>();
   }
}