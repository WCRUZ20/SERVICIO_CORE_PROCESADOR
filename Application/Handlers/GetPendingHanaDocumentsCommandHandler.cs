using Application.Abstractions;
using Application.Commands;
using Application.Interfaces.HANA;
using Domain.SAP;
using Microsoft.Extensions.Logging;
using SAPbobsCOM;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Handlers
{
    public class GetPendingHanaDocumentsCommandHandler 
        : ICommandHandler<GetPendingHanaDocumentsCommand, IEnumerable<SapDrivinTable>>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly ILogger<GetPendingHanaDocumentsCommandHandler> _logger;

        public GetPendingHanaDocumentsCommandHandler(
            IHanaRepository hanaRepository,
            ILogger<GetPendingHanaDocumentsCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<SapDrivinTable>> HandleAsync(
            GetPendingHanaDocumentsCommand command)
        {
            _logger.LogInformation(
                "Obteniendo documentos pendientes desde HANA  -> API");

            var documents = await _hanaRepository.GetPendingDocumentsAsync<SapDrivinTableDTO>();

            //_logger.LogInformation(
            //    $"Se encontraron {documents.Count()} documentos pendientes en HANA -> API");
            
            return documents;
        }
    }
}

