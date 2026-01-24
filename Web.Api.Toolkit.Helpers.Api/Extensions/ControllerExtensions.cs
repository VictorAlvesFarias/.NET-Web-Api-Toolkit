using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Toolkit.Helpers.Application.Dtos;
using Web.Api.Toolkit.Helpers.Domain.Interfaces;

namespace Web.Api.Toolkit.Helpers.Api.Extensions
{
    public static class ControllerExtensions
    {
        public static ActionResult<BaseResponse> Result(this ControllerBase controller, BaseResponse result)
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

                return controller.StatusCode(StatusCodes.Status500InternalServerError,new BaseResponse<IEnumerable<T>>
                {
                    Success = false,
                    Errors = new List<ErrorMessage> { new ErrorMessage("Ocorreu um erro interno no servidor") }
                });
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

        public static ActionResult<BaseResponse<T>> Result<T>(this ControllerBase controller, BaseResponse<T> result)
        {
            return controller.Result(result);
        }
    }
}
