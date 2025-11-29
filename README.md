# JSON2CSV

Conversor de JSON para CSV em Blazor usando algoritmo **Abstract Syntax Tree (AST)** com proteções de segurança integradas.

## Tecnologias

- **Blazor WebAssembly**
- **MudBlazor** - Biblioteca de componentes

## Estrutura do Projeto

- `JSON2CSV` - Projeto servidor Blazor
- `JSON2CSV.Client` - Projeto cliente WebAssembly
- `JSON2CSV.Tests` - Testes unitários

## Arquivos de Exemplo

Os arquivos `.json` na raiz do projeto foram usados para validação do algoritmo.

## Segurança

### Vulnerabilidades Identificadas e Proteções

A aplicação foi auditada e implementa proteções contra as seguintes vulnerabilidades:

#### Prevenção de DoS (Denial of Service)

**Billion Laughs Attack**
- Implementada limitação de profundidade máxima de 10 níveis
- Contagem de tokens com limite de 100.000 elementos
- Validação precoce evita parsing de estruturas excessivamente profundas

**Cartesian Product Explosion**
- Limite de 100 elementos por array
- Máximo de 10.000 linhas no CSV final
- Máximo de 500 colunas no CSV final
- Truncamento automático com notificação ao usuário

**Buffer Overflow**
- Limite de tamanho de JSON: 10 MB
- Dispose adequado de recursos JsonDocument
- Verificação de tamanho antes do processamento

#### Proteção contra CSV Injection

**Fórmula Maliciosa em Células**
- Sanitização automática de campos começando com =, +, -, @
- Prefixo com aspas simples para fórmulas Excel/LibreOffice
- Detecção de padrões DDE e CMD
- Escape adequado de aspas duplas em strings

**Proteção de Dados Sensíveis**
- Detecção automática de campos sensíveis (senha, token, api_key, cpf, cnpj, cartão)
- Máscara inteligente mostrando apenas últimos 4 caracteres
- Regex para identificação de padrões sensíveis

**Segurança de Download**
- Sanitização de nomes de arquivo gerados
- Remoção de caracteres inválidos: /, \, :, *, ?, ", <, >, |
- Limite de 100 caracteres para nome do arquivo
- Fallback seguro para nomes inválidos

#### Tratamento de Erro

- Mensagens de erro específicas com linha e posição
- Categorização de códigos de erro
- Captura de exceções: JsonException, ArgumentException, OverflowException
- Sem exposição de stack traces em produção

### Limites de Segurança

| Recurso | Limite | Finalidade |
|---------|--------|------------|
| Profundidade JSON | 10 níveis | Prevenir stack overflow |
| Tamanho JSON | 10 MB | Prevenir exhaustion de memória |
| Elementos JSON | 100.000 tokens | Prevenir parsing excessivo |
| Itens em Array | 100 elementos | Prevenir cardinalidade excessiva |
| Linhas CSV | 10.000 linhas | Prevenir output massivo |
| Colunas CSV | 500 colunas | Prevenir estruturas muito largas |
| Nome Arquivo | 100 caracteres | Prevenir problemas de sistema |

### Testes de Segurança

Arquivo `security-test-payloads.json` contém payloads maliciosos para validação das proteções:

**Billion Laughs**
```json
{"lol": [{"lol": [{"lol": ... 10 níveis ...}]}]}
```

**CSV Injection**
```json
[{"nome": "=CMD|'calc'!A0"}]
```

**Dados Sensíveis**
```json
{"senha": "123456", "token": "abc123"}
```

**Cartesian Explosion**
```json
[{"emails": [...100 itens...], "telefones": [...100 itens...]}]
```
