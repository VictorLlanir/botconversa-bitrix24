namespace Integration.Api.Models
{
    public sealed class IntegrationData
    {
        public string Telefone { get; set; }
        public string Comentario { get; set; }
        public int Vendedor { get; set; }

        public void TratarTelefone()
        {
            var ddd = Telefone.Substring(0, 5);
            var numero = Telefone.Substring(5);

            if (numero.Length == 8)
            {
                numero = "9" + numero;
            }

            Telefone = ddd + numero;
        }
    }
}
