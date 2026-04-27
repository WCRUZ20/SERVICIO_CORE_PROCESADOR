using Application.DTO;
using Domain.SAP;
using Mapster;

namespace Application.Configuration
{
    /// <summary>
    /// Configuración de mapeos de Mapster
    /// </summary>
    public static class MapsterConfig
    {
        /// <summary>
        /// Configura todos los mapeos de la aplicación
        /// </summary>
        public static void ConfigureMappings()
        {
            //// Configuración de mapeo de StockTransferSAP a StockTransferDTO
            //TypeAdapterConfig<StockTransferSAP, StockTransferDTO>
            //    .NewConfig()
            //    .Map(dest => dest.Lines, src => src.StockTransferLines)
            //    .IgnoreIf((src, dest) => src.StockTransferLines == null, dest => dest.Lines);

            //// Configuración de mapeo de StockTransferDTO a StockTransferSAP
            //TypeAdapterConfig<StockTransferDTO, StockTransferSAP>
            //    .NewConfig()
            //    .Map(dest => dest.StockTransferLines, src => src.Lines)
            //    .IgnoreIf((src, dest) => src.Lines == null, dest => dest.StockTransferLines);

            //// Configuración de mapeo de StockTransferLine a StockTransferLineDTO
            //TypeAdapterConfig<StockTransferLine, StockTransferLineDTO>
            //    .NewConfig();

            //// Configuración de mapeo de StockTransferLineDTO a StockTransferLine
            //TypeAdapterConfig<StockTransferLineDTO, StockTransferLine>
            //    .NewConfig();


            // Configuración de mapeo de StockTransferSAP a StockTransferDTO
            //TypeAdapterConfig<SapDrivinTable, SapDrivinTableDTO>
            //    .NewConfig()
                //.Map(dest => dest.Lines, src => src.StockTransferLines)
                /*.IgnoreIf((src, dest) => src.StockTransferLines == null, dest => dest.Lines)*/;

            // Configuración de mapeo de StockTransferDTO a StockTransferSAP
            //TypeAdapterConfig<SapDrivinTableDTO, SapDrivinTable>
            //    .NewConfig()
                //.Map(dest => dest.StockTransferLines, src => src.Lines)
                /*.IgnoreIf((src, dest) => src.Lines == null, dest => dest.StockTransferLines)*/;


        }
    }
}

