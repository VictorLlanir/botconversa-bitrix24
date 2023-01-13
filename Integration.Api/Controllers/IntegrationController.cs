using Bitrix24RestApiClient.Api;
using Bitrix24RestApiClient.Core.Client;
using Bitrix24RestApiClient.Core.Models;
using Integration.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Integration.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class IntegrationController : ControllerBase
{
    private int IdMylena = 13;
    private int IdRenally = 15;
    private int IdMayara = 17;
    private int IdThiago = 19;
    private int IdMarlone = 21;
    private int IdReges = 23;

    private string buscarNegocioPorId = "https://aldocolchoes.bitrix24.com.br/rest/1/zy8vbrcxuztg3zym";
    private string buscarListaDeNegocios = "https://aldocolchoes.bitrix24.com.br/rest/1/6pkzykeoubyhdlhv";
    private string buscarContatoPorId = "https://aldocolchoes.bitrix24.com.br/rest/1/7enf70qbrd0apuin";
    private string buscarListaDeContatos = "https://aldocolchoes.bitrix24.com.br/rest/1/jhmk816gfz2z2et4";
    private string atualizarNegocio = "https://aldocolchoes.bitrix24.com.br/rest/1/20vk0ax6961i2w5j";
    private string buscarCamposDoDeal = "https://aldocolchoes.bitrix24.com.br/rest/1/00d6q0o0a1h9j2c6/";

    private readonly ILogger<Bitrix24Client> _logger;
    public IntegrationController(ILogger<Bitrix24Client> logger)
    {
        _logger = logger;
    }

    [HttpGet("health-check")]
    public IActionResult HealthCheck()
    {
        return Ok("Está funcionando!");
    }


    [HttpPost("alterar-dados")]
    public async Task<IActionResult> AlterarDadosDoNegocio([FromForm] IntegrationData data)
    {
        try
        {
            GravaLog("------------------------------------------", true);
            data.TratarTelefone();
            return await ExecutarTransformacaoDeDados(data);
        }
        catch (Exception)
        {
            return new BadRequestObjectResult(new { Message = "Ocorreu um erro ao processar a requisição." });
        }
    }

    private Bitrix24 InstanciarNovoCliente(string webhookUrl)
    {
        var client = new Bitrix24Client(webhookUrl, _logger);
        return new Bitrix24(client);
    }

    private async Task<IActionResult> ExecutarTransformacaoDeDados(IntegrationData data)
    {
        GravaLog($"Iniciando alteração para o vendedor {data.Vendedor}. Número: {data.Telefone}");
        var buscarListaDeContatosClient = InstanciarNovoCliente(buscarListaDeContatos);
        var contato = (await buscarListaDeContatosClient.Crm.Contacts.List(p => p.AddPhoneFilter(data.Telefone)));

        if (contato?.Result?.Any() == false)
        {
            return new BadRequestObjectResult(new { Message = "Não foi possível encontrar um contato com esse telefone." });
        }

        var contatoId = contato.Result.First().Id;
        GravaLog($"Contato {contatoId} encontrado. Iniciando busca do negócio...");
        var buscarListaDeNegociosClient = InstanciarNovoCliente(buscarListaDeNegocios);
        var negocio = (await buscarListaDeNegociosClient.Crm.Deals.List(p => p.AddFilter(x => x.ContactId, contatoId, FilterOperator.Equal))).Result.FirstOrDefault();

        if (negocio == null)
        {
            return new BadRequestObjectResult(new { Message = "Não foi possível encontrar um negócio para esse contato." });
        }

        GravaLog($"Negócio {negocio.Id} encontrado. Iniciando alteração dos dados...");
        try
        {
            var atualizarNegocioClient = InstanciarNovoCliente(atualizarNegocio);
            var response = await atualizarNegocioClient.Crm.Deals.Update(negocio.Id.Value, x =>
            {
                x.SetField(y => y.AssignedById, data.Vendedor);
                x.SetField(y => y.Comments, data.Comentario);
            });

            if (response.Result == true)
            {
                GravaLog($"Negócio {negocio.Id} atualizado com sucesso!", true);
                return new OkObjectResult(new { Message = $"Negócio atualizado com sucesso." });
            }
            else
            {
                return new BadRequestObjectResult(new { Message = $"Não foi possível atualizar o negócio {negocio.Id}." });
            }

        }
        catch
        {
            return new BadRequestObjectResult(new { Message = "Não foi possível atualizar o negócio." });
        }
    }

    private void GravaLog(string mensagem, bool newLine = false)
    {
        Console.WriteLine($"{DateTime.Now}: {mensagem}");

        if (newLine)
        {
            Console.WriteLine("");
        }
    }
}
