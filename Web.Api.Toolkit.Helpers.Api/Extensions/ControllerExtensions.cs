using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Toolkit.Helpers.Application.Dtos;

namespace Web.Api.Toolkit.Helpers.Api.Extensions
{
    public static class ControllerExtensions
    {
        public static ActionResult<TResponse> Result<TResponse>(this ControllerBase controller, TResponse result) where TResponse : BaseResponse, new()
        {
            try
            {
                if (!result.Success && !result.Errors.Any())
                {
                    result.AddError(new ErrorMessage("Ocorreu um erro interno no servidor", StatusCodes.Status500InternalServerError));
                }

                if (result.Success)
                {
                    return controller.Ok(result);
                }

                return controller.StatusCode(result.Errors.First().StatusCode, result);
            }
            catch (Exception ex)
            {
                var errorResponse = new TResponse();

                errorResponse.AddError(new ErrorMessage("Ocorreu um erro interno no servidor", StatusCodes.Status500InternalServerError));
                errorResponse.AddException(new ErrorMessage(ex.Message));

                return controller.StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }
    }
}