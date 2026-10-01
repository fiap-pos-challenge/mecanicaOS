## O que muda

<!-- Descreva a mudança e o motivo. -->

Closes #

## Checklist (Definition of Done)

- [ ] Testes do comportamento novo e de cada saída de erro (unitário, integração ou funcional)
- [ ] Nenhum `if` de regra de negócio no handler (a regra fica no domínio)
- [ ] Nenhuma mensagem de retorno hard-coded: erros e sucessos vêm dos catálogos (`…Errors`, `…Messages`)
- [ ] Comentários só explicam o porquê (XML comments apenas em controllers e DTOs)
- [ ] Endpoint com policy explícita, XML comment e respostas documentadas no Swagger
- [ ] Nada sigiloso em log (senha, hash, JWT, token de orçamento, CPF/CNPJ, e-mail, telefone, placa, corpo de requisição)
- [ ] `.env.example`, README e `docs/ddd/linguagem-ubiqua.md` atualizados, se aplicável
- [ ] Se o modelo EF mudou, a migration foi gerada e o PR avisa
