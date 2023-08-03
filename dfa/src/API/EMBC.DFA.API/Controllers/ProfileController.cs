using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AutoMapper;
using EMBC.DFA.API.ConfigurationModule.Models;
using EMBC.DFA.API.ConfigurationModule.Models.Dynamics;
using EMBC.DFA.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using NJsonSchema.Annotations;

namespace EMBC.DFA.API.Controllers
{
    [Route("api/profiles")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IHostEnvironment env;
        private readonly IMapper mapper;
        private readonly IConfigurationHandler handler;

        private string currentUserId => User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        public ProfileController(
            IHostEnvironment env,
            IMapper mapper,
            IConfigurationHandler handler)
        {
            this.env = env;
            this.mapper = mapper;
            this.handler = handler;
        }

        /// <summary>
        /// Get the current logged in user's profile
        /// </summary>
        /// <returns>Currently logged in user's profile</returns>
        [HttpGet("current")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Profile>> GetProfile()
        {
            var userId = currentUserId;
            //var profile = mapper.Map<Profile>(await evacuationSearchService.GetRegistrantByUserId(userId));
            //var profile = null;
            //if (profile == null)
            //{
            //    //try get BCSC profile
            //    profile = GetUserFromPrincipal();
            //}
            var profile = await handler.HandleGetUser(userId);
            if (profile == null)
            {
                //try get BCSC profile
                profile = GetUserFromPrincipal();
            }
            if (profile == null) return NotFound(userId);
            return Ok(profile);
        }

        /// <summary>
        /// check if user exists or not
        /// </summary>
        /// <returns>true if existing user, false if a new user</returns>
        [HttpGet("current/exists")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<bool>> GetDoesUserExists()
        {
            var userId = currentUserId;
            var profile = await handler.HandleGetUser(userId);
            return Ok(profile != null);
        }

        /// <summary>
        /// Create or update the current user's profile
        /// </summary>
        /// <param name="profile">The profile information</param>
        /// <returns>profile id</returns>
        [HttpPost("current")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<string>> AddContact(Profile profile)
        {
            profile.BCServiceCardId = currentUserId;
            var mappedProfile = mapper.Map<dfa_appcontact>(profile);
            if (profile == null) return BadRequest("Profile details cannot be empty!");

            var contactId = await handler.HandleContact(mappedProfile);
            return Ok(contactId);
        }

        /// <summary>
        /// Get the logged in user's profile and conflicts with the data that came from the authenticating identity provider
        /// </summary>
        /// <returns>The current user's profile, the identity provider's profile and the detected conflicts</returns>
        [HttpGet("current/conflicts")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<ProfileDataConflict>>> GetProfileConflicts()
        {
            var userId = currentUserId;

            var profile = await handler.HandleGetUser(userId);
            if (profile == null) return NotFound(userId);

            var userProfile = GetUserFromPrincipal();
            var conflicts = ProfilesConflictDetector.DetectConflicts(mapper.Map<Profile>(profile), userProfile);
            return Ok(conflicts);
        }

        private Profile GetUserFromPrincipal()
        {
            if (!User.HasClaim(c => c.Type.Equals("userInfo", System.StringComparison.OrdinalIgnoreCase))) return null;
            var userProfile = BcscUserInfoMapper.MapBcscUserInfoToProfile(User.Identity?.Name, JsonDocument.Parse(User.FindFirstValue("userInfo")));
            return userProfile;
        }
    }

    /// <summary>
    /// User's profile
    /// </summary>
    public class Profile
    {
        public string? Id { get; set; }

        public PersonDetails PersonalDetails { get; set; }

        public ContactDetails ContactDetails { get; set; }

        public Address PrimaryAddress { get; set; }

        public Address MailingAddress { get; set; }

        public string IsMailingAddressSameAsPrimaryAddress { get; set; }

        public string? BCServiceCardId { get; set; }
    }

    /// <summary>
    /// Base class for profile data conflicts
    /// </summary>
    //[JsonConverter(typeof(PolymorphicJsonConverter<ProfileDataConflict>))]
    [KnownType(typeof(DateOfBirthDataConflict))]
    [KnownType(typeof(NameDataConflict))]
    [KnownType(typeof(AddressDataConflict))]
    public abstract class ProfileDataConflict
    {
        [Required]
        public abstract string DataElementName { get; }
    }

    /// <summary>
    /// Date of birth data conflict
    /// </summary>
    public class DateOfBirthDataConflict : ProfileDataConflict
    {
        [Required]
        public override string DataElementName => nameof(DateOfBirthDataConflict);

        public string ConflictingValue { get; set; }

        public string OriginalValue { get; set; }
    }

    /// <summary>
    /// Name data conflict
    /// </summary>
    public class NameDataConflict : ProfileDataConflict
    {
        [Required]
        public override string DataElementName => nameof(NameDataConflict);

        public ProfileName ConflictingValue
        { get; set; }

        public ProfileName OriginalValue
        { get; set; }
    }

    public class ProfileName
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    /// <summary>
    /// Address data conflict
    /// </summary>
    public class AddressDataConflict : ProfileDataConflict
    {
        [Required]
        public override string DataElementName => nameof(AddressDataConflict);

        public Address ConflictingValue { get; set; }

        public Address OriginalValue { get; set; }
    }

    public class InviteRequest
    {
        [Required]
        public string FileId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class InviteToken
    {
        [Required]
        public string Token { get; set; }
    }
}
