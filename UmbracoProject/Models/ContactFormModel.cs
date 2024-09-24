namespace UmbracoProject.Models;

public class ContactFormModel
{

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Message { get; set; }
}
