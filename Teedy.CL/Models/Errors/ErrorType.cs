namespace Teedy.CL.Models.Errors
{
    public enum ErrorType
    {
        ForbiddenError = 101,
        ValidationError,
        IllegalTagName,
        ParentNotFound,
        CircularReference,
        NotFound
    }
}
