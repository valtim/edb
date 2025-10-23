-- Configurações iniciais do banco de dados
SET NAMES utf8mb4;
SET time_zone = '+00:00';
SET foreign_key_checks = 0;
SET sql_mode = 'NO_AUTO_VALUE_ON_ZERO';

-- Usar o banco de dados criado
USE diario_bordo;

-- Criar índices adicionais para performance (serão aplicados após migrações)
-- Os índices principais serão criados pelas migrações do Entity Framework

-- Configurar timezone padrão
SET GLOBAL time_zone = '+00:00';

-- Configurações de performance
SET GLOBAL innodb_buffer_pool_size = 1073741824; -- 1GB
SET GLOBAL max_connections = 200;

-- Criar usuário para monitoramento (opcional)
CREATE USER IF NOT EXISTS 'monitor'@'%' IDENTIFIED BY 'monitor123';
GRANT PROCESS, REPLICATION CLIENT ON *.* TO 'monitor'@'%';
GRANT SELECT ON performance_schema.* TO 'monitor'@'%';

-- Configurações de charset para o banco
ALTER DATABASE diario_bordo CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

FLUSH PRIVILEGES;
