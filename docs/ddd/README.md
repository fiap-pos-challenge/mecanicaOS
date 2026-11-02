# Documentação DDD — mecanicaOS

Documentação de Domain-Driven Design da solução entregue. Todos os nomes de comandos, agregados, eventos, políticas, rotas e testes citados existem no código deste repositório.

| Arquivo | Conteúdo |
|---|---|
| [event-storming.pdf](event-storming.pdf) | Event Storming da criação da O.S., do acompanhamento da O.S. (com a máquina de status) e da gestão de peças e insumos |
| [diagramas.pdf](diagramas.pdf) | Subdomínios, mapa de contextos, modelo de domínio (agregados, entidades e objetos de valor), Domain Storytelling as-is e to-be, e modelo C4 |
| [linguagem-ubiqua.md](linguagem-ubiqua.md) | Linguagem ubíqua e glossário, com o nome de cada termo no código, atores, agregados, objetos de valor, comandos, eventos, políticas e bounded contexts |
| [rastreabilidade.md](rastreabilidade.md) | Relação entre cada passo do Event Storming e o código (comando, rota, agregado, evento, política e teste) |
| [fontes/](fontes/) | Fontes HTML dos dois PDFs |
| [miro/](miro/) | Os três Event Storming em CSV (uma célula = um post-it), para montar o board do Miro igual aos PDFs |

**Board no Miro:** https://miro.com/app/board/uXjVESyPi8o=/?share_link_id=587184665645

## Resumo do modelo

- **Bounded contexts** (pastas de mesmo nome em `src/Oficina.Domain`, `Application` e `Infrastructure`): `ServiceOrders` (principal), `Customers`, `Catalog`, `Inventory` (suporte), `Identity` (genérico) e `Common` (shared kernel).
- **Agregados:** `ServiceOrder` (com `ServiceItem`, `PartItem`, `Quote`, `StatusHistory`), `Customer`, `Vehicle`, `Service`, `Part` (com `StockMovement`) e `User`.
- **Eventos de domínio:** `ServiceOrderOpened`, `QuoteSent`, `QuoteApproved`, `QuoteRejected`, `ServiceOrderCompleted`, gravados no Outbox na mesma transação do commit.
- **Políticas:** `ServiceOrderOpeningPolicy`, `QuoteApprovalPolicy`, `RegistrationDeletionPolicy`, `UserAdministrationPolicy`.
- **Status da O.S.:** Recebida → Em diagnóstico → Aguardando aprovação → Em execução → Finalizada → Entregue (rejeição volta para Em diagnóstico).

## Como gerar os PDFs de novo

Depois de editar um arquivo em `fontes/`, rode na raiz do repositório (Chrome ou Edge instalados):

```bash
chrome --headless=new --no-pdf-header-footer --print-to-pdf=docs/ddd/event-storming.pdf docs/ddd/fontes/event-storming.html
chrome --headless=new --no-pdf-header-footer --print-to-pdf=docs/ddd/diagramas.pdf docs/ddd/fontes/diagramas.html
```

Também dá para abrir o HTML no navegador e usar **Imprimir → Salvar como PDF** (A3, paisagem).

## Como deixar o Miro igual aos PDFs

Os PDFs e as fontes em `fontes/` são a referência. O board do Miro deve ter o mesmo conteúdo, na mesma ordem, para não haver divergência no vídeo.

1. **Frames:** crie um frame para cada página, com o título do PDF:
   - `Event Storming 1 · Criação da Ordem de Serviço`;
   - `Event Storming 2 · Acompanhamento da Ordem de Serviço`;
   - `Event Storming 3 · Gestão de peças e insumos`;
   - as cinco páginas do `diagramas.pdf`: `1 · Subdomínios`, `2 · Mapa de contextos`, `3 · Modelo de domínio`, `4 e 5 · Domain Storytelling (as-is e to-be)` e `6 · Arquitetura (C4)`.
2. **Event Storming:**
   - Abra o CSV do frame em [miro/](miro/) no Excel ou no Google Sheets.
   - Selecione as células de uma raia (sem a primeira coluna) e cole no board: o Miro cria um post-it por célula, mantendo a ordem.
   - Pinte cada raia com a cor da legenda do PDF:

     | Raia | Cor |
     |---|---|
     | Ator | amarelo-claro |
     | Read model | verde |
     | Comando | azul |
     | Agregado | amarelo-palha |
     | Evento | laranja |
     | Política / regra | lilás |
     | Hot spot | vermelho |
3. **Diagramas:** arraste o `diagramas.pdf` para o board (**Upload → PDF**). Cada página vira uma imagem; coloque cada uma no frame correspondente. Se preferir, arraste também o `event-storming.pdf` ao lado dos post-its, como referência.
4. **Apague do board** o que não aparece nos PDFs: post-its antigos, nomes de comandos ou eventos que não existem no código e diagramas de versões anteriores.
5. **Compartilhamento:** em **Share**, escolha *Anyone with the link → Can view* e teste o link numa janela anônima.

Depois de mudar uma fonte em `fontes/event-storming.html`, gere os PDFs (comandos acima) e os CSVs com `python docs/ddd/fontes/miro-csv.py docs/ddd/fontes/event-storming.html docs/ddd/miro`, e atualize o board.
