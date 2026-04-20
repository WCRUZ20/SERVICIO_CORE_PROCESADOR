-- Script SQL para crear la tabla histórica de transferencias en HANA
-- Ajusta según tu esquema y necesidades específicas

CREATE TABLE STOCK_TRANSFERS_HIST (
    Id BIGINT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    DocEntry INTEGER NOT NULL,
    DocNum INTEGER,
    DocDate DATE,
    FromWarehouse NVARCHAR(50),
    ToWarehouse NVARCHAR(50),
    CardCode NVARCHAR(50),
    CardName NVARCHAR(200),
    TransferData NCLOB,  -- JSON completo de la transferencia
    IsProcessed SMALLINT DEFAULT 0,  -- 0 = Pendiente, 1 = Procesado
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_DocEntry UNIQUE (DocEntry)
);

-- Índice para mejorar las consultas de transferencias pendientes
CREATE INDEX IDX_STOCK_TRANSFERS_PENDING 
    ON STOCK_TRANSFERS_HIST (IsProcessed, DocDate, DocEntry);

-- Índice para búsquedas por DocEntry
CREATE INDEX IDX_STOCK_TRANSFERS_DOCENTRY 
    ON STOCK_TRANSFERS_HIST (DocEntry);

-- Comentarios en la tabla
COMMENT ON TABLE STOCK_TRANSFERS_HIST IS 'Tabla histórica para almacenar transferencias de stock desde SAP';
COMMENT ON COLUMN STOCK_TRANSFERS_HIST.TransferData IS 'JSON completo de la transferencia serializada';
COMMENT ON COLUMN STOCK_TRANSFERS_HIST.IsProcessed IS '0 = Pendiente de enviar a API, 1 = Ya procesada';






