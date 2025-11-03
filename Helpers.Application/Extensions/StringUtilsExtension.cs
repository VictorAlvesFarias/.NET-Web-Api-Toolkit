namespace Web.Api.Toolkit.Web.Api.Toolkit.Helpers.Application.Extensions
{
    public static class StringUtilsExtension
    {
        public static string ToDocument(this string data, string type, string defaultValue = null)
        {
            if (type == "br")
            {
                if (string.IsNullOrWhiteSpace(data) && defaultValue is not null)
                {
                    return defaultValue;
                }
                else if (data.Length == 11)
                {
                    var response = string.Format(@"{0:00\.000\.000\-00}", data);

                    return response;
                }
                else if (data.Length == 14)
                {
                    var response = string.Format(@"{0:00\.000\.000\/0000\-00}", data);

                    return response;
                }
                else
                {
                    throw new Exception("Campo não é valida para um CPF/CNPJ.");
                }
            }
            else
            {
                throw new Exception("Campo não é valida para um CPF.");
            }
        }
        public static string ToPhone(this string data, string type, string defaultValue = null)
        {

            if (type == "br")
            {
                if (string.IsNullOrWhiteSpace(data) && defaultValue is not null)
                {
                    return defaultValue;
                }
                else if (data.Length == 10) // Formato: (XX) XXXX-XXXX
                {
                    data = new string(data.Where(char.IsDigit).ToArray());

                    return string.Format("({0}) {1}-{2}", data.Substring(0, 2), data.Substring(2, 4), data.Substring(6, 4));
                }
                else if (data.Length == 11) // Formato: (XX) 9XXXX-XXXX
                {
                    data = new string(data.Where(char.IsDigit).ToArray());

                    return string.Format("({0}) {1}-{2}", data.Substring(0, 2), data.Substring(2, 5), data.Substring(7, 4));
                }
                else
                {
                    throw new Exception("Número de telefone inválido.");
                }
            }
            else
            {
                throw new Exception("Campo não é valida para um CPF.");
            }
        }
    }
}
