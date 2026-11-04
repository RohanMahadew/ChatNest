using System.ComponentModel.DataAnnotations;

namespace PL.Models.DTOs
{
    public class DirectChannelCreationInputDTO
    {
        [Required (ErrorMessage = "CreatorId is required")]
        public int CreatorId { get; set; }


        [Required (ErrorMessage = "ParticipantId is required")]
        public int ParticipantId { get; set; }
    }
}
