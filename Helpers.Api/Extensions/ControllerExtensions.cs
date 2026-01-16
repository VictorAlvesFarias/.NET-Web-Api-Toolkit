using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Toolkit.Helpers.Application.Dtos;
using Web.Api.Toolkit.Helpers.Domain.Interfaces;

namespace Web.Api.Toolkit.Helpers.Api.Extensions
{
    public static class ControllerExtensions
    {
        public static ActionResult<BaseResponse<T>> Result<T>(this ControllerBase controller, BaseResponse<T> result)
        {
            try
            {
                if (result.Success)
                {
                    return controller.Ok(result);
                }
                else if (result.Errors.Count > 0)
                {
                    return controller.StatusCode(result.Errors.First().StatusCode, result);
                }

                return controller.StatusCode(StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                return controller.StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse<IEnumerable<T>>
                {
                    Success = false,
                    Errors = new List<ErrorMessage> { new ErrorMessage("Ocorreu um erro interno no servidor") },
                    Exceptions = new List<ErrorMessage> { new ErrorMessage(ex.Message) }
                });
            }
        }
        public static ActionResult<DefaultResponse> DefaultResult(this ControllerBase controller, DefaultResponse result)
        {
            try
            {
                if (result.Success)
                {
                    return controller.Ok(result);
                }
                else if (result.Errors.Count > 0)
                {
                    return controller.StatusCode(result.Errors.First().StatusCode, result);
                }

                return controller.StatusCode(StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                return controller.StatusCode(StatusCodes.Status500InternalServerError, new DefaultResponse
                {
                    Success = false,
                    Errors = new List<ErrorMessage> { new ErrorMessage("Ocorreu um erro interno no servidor") },
                    Exceptions = new List<ErrorMessage> { new ErrorMessage(ex.Message) }
                });
            }
        }
        public static IActionResult FileResult<T>(this ControllerBase controller, BaseResponse<T> result, bool enableRangeProcessing) where T : IFileBase
        {
            try
            {
                if (result.Success)
                {
                    var fileName = "download.zip";
                    
                    // Tentar obter o nome do arquivo se o tipo tiver a propriedade Name
                    var nameProperty = result.Data.GetType().GetProperty("Name");
                    if (nameProperty != null)
                    {
                        var nameValue = nameProperty.GetValue(result.Data) as string;
                        if (!string.IsNullOrEmpty(nameValue))
                        {
                            fileName = nameValue;
                        }
                    }

                    return controller.File(result.Data.Bytes, result.Data.MimeType, fileName, enableRangeProcessing);
                }
                else if (result.Errors.Count > 0)
                {
                    return controller.StatusCode(result.Errors.First().StatusCode, result);
                }

                return controller.StatusCode(StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                return controller.StatusCode(StatusCodes.Status500InternalServerError, new DefaultResponse
                {
                    Success = false,
                    Errors = new List<ErrorMessage> { new ErrorMessage("Ocorreu um erro interno no servidor") },
                    Exceptions = new List<ErrorMessage> { new ErrorMessage(ex.Message) }
                });
            }
        }

    }
}
