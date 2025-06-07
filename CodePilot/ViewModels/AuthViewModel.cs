using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CodePilot.ViewModels
{
    public class AuthViewModel
    {
        [Required]
        [Remote("CheckUsername", "Account", ErrorMessage = "This userame is taken!")]
        public string regUsername { get; set; }

        [Required]
        [EmailAddress]
        [Remote("CheckEmail", "Account", ErrorMessage = "This email is registered by another account!")]
        public string regEmail { get; set; }

        [Required]
        public string logUsername { get; set; }

        [Required]
        [EmailAddress]
        public string logEmail { get; set; }

        [Required]
        [DataType(DataType.Password)]
        //[RegularExpression(@"(?=.*\d)(?=.*[a-z])(?=.*[A-Z]).{8,}", ErrorMessage = "Invalid password type!")]
        [RegularExpression(@"^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[\W_]).{8,}$", ErrorMessage = "Invalid Password! (at least 8 characters, including uppercase, lowercase, digit, and one special character)")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password confirmation does not match.")]
        public string ConfirmPassword { get; set; }
    }
}
