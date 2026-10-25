# Documentação DDD — mecanicaOS

Documentação de Domain-Driven Design da solução entregue. Os diagramas são editados no draw.io; as fontes ficam em [docs/arquitetura/](../arquitetura/) e as cópias em PDF e SVG ficam nesta pasta.

| Arquivo | Conteúdo |
|---|---|
| [event-storming.pdf](event-storming.pdf) | Event Storming da criação da O.S., do acompanhamento da O.S. e da gestão de peças e insumos |
| [diagramas.pdf](diagramas.pdf) | Subdomínios, mapa de contextos, modelo de domínio, Domain Storytelling (as-is e to-be) e modelo C4 |
| [linguagem-ubiqua.md](linguagem-ubiqua.md) | Linguagem ubíqua e glossário, com o nome de cada termo no código, atores, agregados, objetos de valor, comandos, eventos, políticas e bounded contexts |
| [rastreabilidade.md](rastreabilidade.md) | Relação entre cada passo do Event Storming e o código (comando, handler, agregado, evento e política) |

## Abrir no draw.io (sem login e sem pedir permissão)

Os links abaixo abrem a versão da branch `main` no visualizador do diagrams.net, enquanto o repositório estiver público.

| Diagrama | Fonte | Visualizar |
|---|---|---|
| Event Storming | [EventStorming_mecanicaOS.drawio](../arquitetura/EventStorming_mecanicaOS.drawio) | [Abrir no draw.io](https://app.diagrams.net/?lightbox=1#Uhttps%3A%2F%2Fraw.githubusercontent.com%2Ffiap-pos-challenge%2FmecanicaOS%2Fmain%2Fdocs%2Farquitetura%2FEventStorming_mecanicaOS.drawio) |
| Subdomínios, mapa de contextos e modelo de domínio | [DDD_mecanicaOS.drawio](../arquitetura/DDD_mecanicaOS.drawio) | [Abrir no draw.io](https://app.diagrams.net/?lightbox=1#Uhttps%3A%2F%2Fraw.githubusercontent.com%2Ffiap-pos-challenge%2FmecanicaOS%2Fmain%2Fdocs%2Farquitetura%2FDDD_mecanicaOS.drawio) |
| Domain Storytelling | [DomainStorytelling_mecanicaOS.drawio](../arquitetura/DomainStorytelling_mecanicaOS.drawio) | [Abrir no draw.io](https://app.diagrams.net/?lightbox=1#Uhttps%3A%2F%2Fraw.githubusercontent.com%2Ffiap-pos-challenge%2FmecanicaOS%2Fmain%2Fdocs%2Farquitetura%2FDomainStorytelling_mecanicaOS.drawio) |
| Modelo C4 | [C4_Oficina.drawio](../arquitetura/C4_Oficina.drawio) | [Abrir no draw.io](https://app.diagrams.net/?lightbox=1#Uhttps%3A%2F%2Fraw.githubusercontent.com%2Ffiap-pos-challenge%2FmecanicaOS%2Fmain%2Fdocs%2Farquitetura%2FC4_Oficina.drawio) |

## Event Storming

Os três fluxos pedidos no enunciado: criação da O.S., acompanhamento da O.S. e gestão de peças e insumos. Cada post-it de comando, agregado, evento e política aparece com o nome usado no código.

### Criação da O.S.

![Criação da O.S.](img/EventStorming_mecanicaOS-1-criacao-da-o-s.svg)

### Acompanhamento da O.S.

![Acompanhamento da O.S.](img/EventStorming_mecanicaOS-2-acompanhamento-da-o-s.svg)

### Peças e insumos

![Peças e insumos](img/EventStorming_mecanicaOS-3-pecas-e-insumos.svg)

## Subdomínios, mapa de contextos e modelo de domínio

Classificação dos subdomínios (principal, suporte e genérico), relação entre os bounded contexts e agregados, entidades e objetos de valor.

### Subdomínios

![Subdomínios](img/DDD_mecanicaOS-1-subdominios.svg)

### Mapa de contextos

![Mapa de contextos](img/DDD_mecanicaOS-2-mapa-de-contextos.svg)

### Modelo de domínio

![Modelo de domínio](img/DDD_mecanicaOS-3-modelo-de-dominio.svg)

## Domain Storytelling

Como a oficina trabalha hoje (as-is), como passa a trabalhar com o sistema (to-be) e a comparação entre os dois.

### As-is 1 · Cadastro de cliente e veículo

![As-is 1 · Cadastro de cliente e veículo](img/DomainStorytelling_mecanicaOS-1-as-is-1-cadastro-de-cliente-e-veiculo.svg)

### As-is 2 · Cadastro de insumos ou materiais

![As-is 2 · Cadastro de insumos ou materiais](img/DomainStorytelling_mecanicaOS-2-as-is-2-cadastro-de-insumos-ou-materiais.svg)

### As-is 3 · Cadastro de serviços prestados

![As-is 3 · Cadastro de serviços prestados](img/DomainStorytelling_mecanicaOS-3-as-is-3-cadastro-de-servicos-prestados.svg)

### As-is 4 · Orçamento da Ordem de Serviço

![As-is 4 · Orçamento da Ordem de Serviço](img/DomainStorytelling_mecanicaOS-4-as-is-4-orcamento-da-ordem-de-servico.svg)

### As-is 5 · Análise do orçamento pelo cliente

![As-is 5 · Análise do orçamento pelo cliente](img/DomainStorytelling_mecanicaOS-5-as-is-5-analise-do-orcamento-pelo-cliente.svg)

### As-is 6 · Execução da Ordem de Serviço

![As-is 6 · Execução da Ordem de Serviço](img/DomainStorytelling_mecanicaOS-6-as-is-6-execucao-da-ordem-de-servico.svg)

### As-is 7 · Entrega do veículo

![As-is 7 · Entrega do veículo](img/DomainStorytelling_mecanicaOS-7-as-is-7-entrega-do-veiculo.svg)

### To-be 1 · Abertura da O.S.

![To-be 1 · Abertura da O.S.](img/DomainStorytelling_mecanicaOS-8-to-be-1-abertura-da-o-s.svg)

### To-be 2 · Diagnóstico e envio para orçamento

![To-be 2 · Diagnóstico e envio para orçamento](img/DomainStorytelling_mecanicaOS-9-to-be-2-diagnostico-e-envio-para-orcamento.svg)

### To-be 3 · Orçamento: montagem, envio e decisão pelo portal

![To-be 3 · Orçamento: montagem, envio e decisão pelo portal](img/DomainStorytelling_mecanicaOS-10-to-be-3-orcamento-montagem-envio-e-decisao-pelo-portal.svg)

### To-be 4 · Execução, baixa de estoque e finalização

![To-be 4 · Execução, baixa de estoque e finalização](img/DomainStorytelling_mecanicaOS-11-to-be-4-execucao-baixa-de-estoque-e-finalizacao.svg)

### To-be 5 · Pagamento Pix simulado, recibo e entrega

![To-be 5 · Pagamento Pix simulado, recibo e entrega](img/DomainStorytelling_mecanicaOS-12-to-be-5-pagamento-pix-simulado-recibo-e-entrega.svg)

### To-be 6 · Reposição de estoque

![To-be 6 · Reposição de estoque](img/DomainStorytelling_mecanicaOS-13-to-be-6-reposicao-de-estoque.svg)

### As-is × To-be

![As-is × To-be](img/DomainStorytelling_mecanicaOS-14-as-is-to-be.svg)

## Modelo C4

Contexto, containers e componentes da API.

### Contexto

![Contexto](img/C4_Oficina-1-contexto.svg)

### Containers

![Containers](img/C4_Oficina-2-containers.svg)

### Componentes da API

![Componentes da API](img/C4_Oficina-3-componentes-api.svg)

## Como gerar de novo

Depois de editar um `.drawio` em `docs/arquitetura/`, rode na raiz do repositório (precisa de `npm ci` em `web/` e de acesso à internet para carregar o visualizador do draw.io):

```bash
node scripts/docs/export-diagrams.mjs
```

O script renderiza cada página com o visualizador oficial do draw.io num Chromium do Playwright, grava os SVGs em `docs/ddd/img/` e junta as páginas em `event-storming.pdf` e `diagramas.pdf`. Os diagramas de DDD e Domain Storytelling são gerados por `node scripts/docs/generate-ddd-diagrams.mjs`.
