using Helpers.Application.Dtos;
using Helpers.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Helpers.Api.Extensions
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
        public static IActionResult FileResult<T>(this ControllerBase controller, BaseResponse<T> result) where T : IFileBase
        {
            try
            {
                if (result.Success)
                {
                    return controller.File(result.Data.GetBytes(), result.Data.GetMimeType());
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
