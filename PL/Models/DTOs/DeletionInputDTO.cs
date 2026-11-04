using System.ComponentModel.DataAnnotations;

namespace PL.Models.DTOs
{
    public class DeletionInputDTO
    {
        [Required(ErrorMessage = "Id is required")]
        public int Id;
    }
}
