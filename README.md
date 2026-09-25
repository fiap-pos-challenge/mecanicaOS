# Oficina — Sistema Integrado de Atendimento e Execução de Serviços

MVP do back-end de uma oficina mecânica, desenvolvido no Tech Challenge da Pós Tech SOAT (Software Architecture), Fase 1.

## Objetivo

Organizar o atendimento, o diagnóstico, a execução e a entrega de veículos. Hoje esse processo é feito com anotações manuais e planilhas. O sistema permite:

- abrir e acompanhar ordens de serviço (OS);
- gerar orçamentos automaticamente e registrar a aprovação do cliente;
- controlar o estoque de peças e insumos;
- manter o histórico de clientes e veículos.

## Descrição da solução

API RESTful monolítica, organizada em camadas e modelada com Domain-Driven Design.

**Ordem de Serviço:** passa pelos status Recebida, Em diagnóstico, Aguardando aprovação, Em execução, Finalizada e Entregue. Cada mudança de status é consequência de uma ação no sistema.

**Cliente:** não faz login. Acompanha a OS pelo número e pelo CPF/CNPJ, e aprova ou reprova o orçamento por um link com token.

**Equipe da oficina:** Admin, Atendente e Mecânico usam as rotas administrativas, protegidas por JWT.

## Arquitetura e tecnologias

.NET 10, ASP.NET Core Web API, PostgreSQL 16, Entity Framework Core, Riok.Mapperly, JWT, Swagger, xUnit, Testcontainers, Docker e GitHub Actions.

Os detalhes estão em:

- [instructions/arquitetura.md](instructions/arquitetura.md): camadas, domínio, fluxos, API e persistência.
- [instructions/padrao-desenvolvimento.md](instructions/padrao-desenvolvimento.md): convenções de código, testes, Git flow e CI.

## Estrutura do repositório

```
docs/
  enunciado/      enunciado oficial do Tech Challenge
  arquitetura/    diagramas de fluxos, rotas, permissões e modelo de dados
instructions/     arquitetura e padrão de desenvolvimento do grupo
```
