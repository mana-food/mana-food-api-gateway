# API Gateway - Maná Food

## 1. Introdução

Este projeto implementa um API Gateway para o sistema Mana Food, atuando como ponto de entrada único para os microsserviços da aplicação. O gateway é responsável por rotear requisições, implementar segurança e fornecer uma camada de abstração para os serviços backend.

## 2. Objetivo

O objetivo principal deste API Gateway é centralizar o gerenciamento de requisições HTTP, fornecendo:
- Roteamento inteligente de requisições
- Autenticação e autorização centralizadas

## 3. Arquitetura

O projeto utiliza uma arquitetura de microsserviços, onde o API Gateway atua como:
- **Proxy reverso**: Encaminha requisições para os microsserviços apropriados
- **Agregador**: Combina respostas de múltiplos serviços quando necessário
- **Gateway de segurança**: Valida tokens e permissões antes de rotear requisições

## 4. Tecnologias Utilizadas

- **Runtime**: .NET 9.0 (ou superior)
- **Framework**: ASP.NET Core
- **Linguagem**: C#
- **Protocolo**: HTTP/HTTPS
- **Autenticação**: JWT Bearer Token

## 5. Instalação

### 5.1 Pré-requisitos

- .NET SDK 9.0 ou superior
- Visual Studio 2022 / Visual Studio Code / Rider
- Acesso aos microsserviços backend

### 5.2 Passos de Instalação

```bash
# Clone o repositório
git clone https://github.com/mana-food/mana-food-api-gateway.git

# Navegue até o diretório
cd mana-food-api-gateway

# Restaure as dependências
dotnet restore

# Execute o projeto
dotnet run --project src/Gateway.csproj 
```

## 6. Estrutura do Projeto

```
mana-food-api-gateway/
├── src/
│   ├── Controllers/        
│   ├── Configurations/     
│   ├── Extensions/         
│   ├── appsettings.json    
│   ├── Program.cs         
│   └── Gateway.csproj    
├── tests/    
└── README.md               
```
