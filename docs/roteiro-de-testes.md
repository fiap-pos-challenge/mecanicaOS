# Roteiro de testes — mecanicaOS

Roteiro curto para testar todas as funcionalidades pelo navegador. Tempo estimado: 40 minutos.

## 1. Subir o sistema

```bash
cp .env.example .env
docker compose up -d --build
docker compose logs seed
```

No Windows, use `copy .env.example .env`. As senhas de desenvolvimento ficam no `.env`.

- Abra **http://localhost:4200**.
- Na primeira subida, o serviço `seed` preenche o banco como uma oficina em uso. O log mostra os usuários e um **link do portal do cliente**.
- Para começar do zero: `docker compose down -v` e `docker compose up -d --build` (apaga o banco de desenvolvimento).

## 2. Usuários de teste

| Perfil | E-mail | Senha |
|---|---|---|
| Administrador | `admin@oficina.local` | `DEV_ADMIN_PASSWORD` do `.env` |
| Gerente | `gerente@oficina.local` | `SEED_USERS_PASSWORD` do `.env` |
| Atendente | `ana.atendente@oficina.local` | `SEED_USERS_PASSWORD` do `.env` |
| Atendente | `bruno.atendente@oficina.local` | `SEED_USERS_PASSWORD` do `.env` |
| Mecânico | `marcos.mecanico@oficina.local` | `SEED_USERS_PASSWORD` do `.env` |
| Mecânico | `paula.mecanica@oficina.local` | `SEED_USERS_PASSWORD` do `.env` |
| Mecânico | `rafael.mecanico@oficina.local` | `SEED_USERS_PASSWORD` do `.env` |

> O login aceita 5 tentativas por minuto por IP. Se aparecer "muitas tentativas", espere 1 minuto.

## 3. Roteiro (marque cada item)

### Acesso e perfis
- [ ] Entrar como **Administrador**: o menu mostra todas as telas.
- [ ] Recarregar a página (F5): continua logado.
- [ ] Sair e entrar como **Mecânico**: abre o **Modo mecânico** e o menu é reduzido.
- [ ] Como Mecânico, abrir `http://localhost:4200/settings`: aparece "acesso negado".

### Painel (Gerente ou Administrador)
- [ ] Ver faturamento, OS abertas, aprovação de orçamentos e valores a receber.
- [ ] Trocar o período (Hoje, 7 dias, 30 dias).
- [ ] Em "Precisa de atenção", clicar **Gerar pedido** na pastilha de freio: o aviso some e o pedido aparece em Estoque > Pedidos de compra.
- [ ] **Personalizar**: esconder um cartão, salvar e recarregar.

### Clientes e veículos (Atendente)
- [ ] Buscar "Joana", abrir a ficha, ver histórico de OS e gasto.
- [ ] **Revelar** o telefone escolhendo um motivo.
- [ ] **Novo cliente** com CEP `01310-100`: o endereço é preenchido sozinho.
- [ ] Cadastrar um veículo para o novo cliente.
- [ ] Filtrar por **Frota** e por situação **OS aberta**.

### Catálogo e estoque (Gerente)
- [ ] Catálogo: alterar o preço de um serviço e ver o histórico.
- [ ] Ver os pacotes e a economia de cada um.
- [ ] Estoque: ver a bateria e a pastilha abaixo do mínimo.
- [ ] Registrar uma **entrada** manual de 10 pastilhas.
- [ ] Inventário: abrir a contagem GERAL, lançar quantidades e enviar.

### Ordem de serviço — fluxo completo
- [ ] Atendente: em **Ordens de serviço**, clicar **Nova OS**, buscar "Luciana", escolher o veículo, descrever o problema e abrir.
- [ ] Mecânico (Marcos): no **Modo mecânico**, abrir a OS, iniciar o atendimento, preencher o checklist, tirar/enviar uma foto, escrever o diagnóstico e **enviar para orçamento** com um serviço e uma peça.
- [ ] Atendente: em **Orçamentos > Novo orçamento**, escolher a OS, ajustar desconto, salvar e **Enviar** por link.
- [ ] Abrir o link do orçamento (aba anônima), confirmar com os 3 últimos dígitos do telefone, **aprovar** os itens e gerar o **Pix**.
- [ ] Mecânico: iniciar e parar o cronômetro e **finalizar** o serviço.
- [ ] Atendente: no quadro, mover a OS para **Entregue**.

### Portal do cliente (dados da carga)
- [ ] Abrir o link mostrado em `docker compose logs seed` e confirmar com `321`.
- [ ] Ver andamento, orçamento e documentos; testar **Recusar orçamento** com um motivo (a OS volta para diagnóstico).
- [ ] Por último, numa aba anônima, errar os dígitos 3 vezes no mesmo link: o link fica bloqueado.
- [ ] Abrir `http://localhost:4200/portal`, digitar o número da OS e o CPF do cliente: a situação aparece.

### Visão do cliente (sem login)

O cliente não tem usuário nem senha. Ele vê tudo pelo **link** que a oficina envia e pela **consulta pública**. Para gravar a visão de um cliente cadastrado (ex.: Joana Prado, OS #4, orçamento aguardando aprovação):

1. **Pegar o link do cliente** (escolha um):
   - no terminal: `docker compose logs seed` e copie a linha "Portal do cliente com orçamento pendente"; ou
   - entre como **Atendente**, abra **Orçamentos**, clique no orçamento **#1 (Joana Prado)** e, no cartão **Envio**, use **Copiar link** (se o link tiver vencido, use **Gerar novo link**).
2. **Abrir como cliente:** abra uma **janela anônima** (Ctrl+Shift+N) e cole o link. Assim a sessão da equipe não aparece no vídeo.
3. **Confirmar a identidade:** digite `321` (os 3 últimos dígitos do telefone da Joana).
4. **Mostrar o que o cliente vê:** nome da oficina, saudação "Olá, Joana", veículo e placa, linha do tempo da OS, fotos do diagnóstico, itens do orçamento e documentos (OS e orçamento).
5. **Aprovar:** desmarque um item para mostrar o total recalculado, clique em **Aprovar itens marcados**.
6. **Pagar com Pix:** clique em **Pagar com Pix** e mostre o código copia-e-cola e o QR code.
7. **Voltar à visão da equipe** (outra janela, como Atendente): a OS #4 foi para **Em execução** no quadro e o sino mostra o aviso de orçamento aprovado.
8. **Consulta pública:** na janela anônima, abra `http://localhost:4200/portal`, informe a OS **4** e o CPF da Joana **100.007.919-89** e clique em **Consultar**.
9. **Verificar um documento:** abra um documento no portal e use o link de verificação, ou abra `http://localhost:4200/v/` seguido do código do documento.

Para mostrar a **recusa** e o **bloqueio**, use outro orçamento da lista (ex.: **#2, Transportes Rota Sul**, telefone terminado em `566`): copie o link, clique em **Recusar orçamento** com um motivo (a OS volta para diagnóstico). Por último, em outra janela anônima, erre os dígitos 3 vezes: o link fica bloqueado.

Outros CPFs da carga para a consulta pública: Carlos Mendes `100.015.838-16` (OS #6) e Fernanda Costa `100.039.595-20` (OS #7).

### Documentos
- [ ] Em **Documentos**, filtrar por **Recibo** e abrir o recibo da OS #11.
- [ ] **Baixar PDF** e **Enviar por e-mail** (o e-mail vira um arquivo `.eml` na pasta `.mail`).
- [ ] Copiar o link de verificação e abri-lo: "Documento autêntico".

### Financeiro e relatórios (Gerente)
- [ ] **Financeiro**: recebido, a receber (OS #7), comissões e formas de pagamento (Balcão e Pix).
- [ ] **Relatórios**: abrir "Faturamento por mês", trocar para gráfico de colunas, **exportar** em Excel e PDF.
- [ ] **Pergunte aos seus dados**: "qual o ticket médio por mecânico no trimestre?".
- [ ] Agendar o envio semanal de um relatório.

### Automações e notificações (Gerente ou Administrador)
- [ ] **Automações**: ver as 4 regras, pausar uma, criar outra a partir de um modelo.
- [ ] Ver a saúde das filas.
- [ ] **Sino** de notificações: abrir, marcar como lidas, ajustar as preferências.

### Equipe e configurações (Administrador)
- [ ] **Equipe**: ver mecânicos com produtividade; editar a comissão de um mecânico.
- [ ] **Convidar** um funcionário: o e-mail com o link vira um arquivo `.eml` na pasta `.mail`.
- [ ] **Configurações**: alterar o horário da oficina (pede a senha de novo), ver sessões ativas e encerrar outra sessão.
- [ ] **Auditoria**: filtrar por área e ver as alterações feitas acima.
- [ ] **Plataforma**: plano, usuários em uso e módulos.

## 4. Testes automáticos

```bash
dotnet test                # back-end: unidade, contrato e funcionais (precisa do Docker)
cd web && npm test         # front-end
cd web && npm run e2e      # ponta a ponta no navegador (sobe uma stack própria na porta 4300)
```
