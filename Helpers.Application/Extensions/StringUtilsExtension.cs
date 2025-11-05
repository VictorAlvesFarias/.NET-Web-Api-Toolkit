using System.Globalization;
using System.Text.RegularExpressions;

namespace Web.Api.Toolkit.Helpers.Application.Extensions
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
        public static bool IsEmail(this string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    string domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            }

            catch (RegexMatchTimeoutException e)
            {
                return false;
            }

            catch (ArgumentException e)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }

            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}
