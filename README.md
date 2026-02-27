# NexBox - Sistema de Gestão e PDV Portátil

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)]()
[![WPF](https://img.shields.io/badge/WPF-Windows_Desktop-0078D4?style=for-the-badge&logo=windows&logoColor=white)]()
[![SQLite](https://img.shields.io/badge/SQLite-Database-003B57?style=for-the-badge&logo=sqlite&logoColor=white)]()

Bem-vindo ao **NexBox** (anteriormente chamado *ProductSync*)! Um ecossistema de gerenciamento de estoque local e Point-of-Sale (Frente de Caixa) projetado do zero para ser moderno, ultraleve e, principalmente, **100% Portátil**.

<br/>

## O Objetivo do Projeto

O objetivo principal desta aplicação foi desafiar a forma como as aplicações desktop são entregues hoje. Em vez de obrigar o cliente a baixar instaladores pesados, instalar serviços de banco de dados na máquina ou iniciar servidores manuais complexos, queríamos algo quase mágico: **um sistema operacional de vendas que pudesse ser executado diretamente de um pendrive**, sem deixar rastros.

Nós unimos o poder robusto e veloz de uma API Back-end .NET com uma interface Desktop rica e amigável feita em C# (WPF). O NexBox é projetado nativamente para pequenos negócios, permitindo que a pessoa espete o USB no computador e, em 1 segundo, tenha todo seu estoque visualizado, gere vendas com recibos digitais e tenha belos gráficos - tudo local (offline first), e o que depender de internet é escalado perfeitamente sob os panos!

<br/>

## As Ferramentas & Tecnologias

Foi utilizado o que há de mais recente no ecossistema C# e .NET 8.0:
- **C# 12 & .NET 8**: Motores principais do sistema de ponta a ponta.
- **WPF (Windows Presentation Foundation)**: Interface visual.
- **Material Design In XAML**: Para fornecer as modernidades gráficas, cantos arredondados, cores em Light Theme (fundo branco, bordas sutis e os destaques *Laranja Corporativo #e6570a* consistentes).
- **SQLite + Entity Framework Core**: Para um banco de dados totalmente embutido dentro da pasta. Sem necessidade de instalar SQL Server, ele cria e manipula um leve `app.db`.
- **APIs REST públicas (UpcItemDb / OpenFoodFacts)**: Emuladores e varredores que buscam instantaneamente a "foto real" de produtos apenas usando o código de barras (EAN).
- **UI-Avatars**: Nosso "fallback" de segurança. Se um produto cadastrado for genérico e não tiver imagem de loja real, o sistema desenha elegantemente na própria tela um avatar laranjado com a inicial geométrica do produto.

<br/>

## A Jornada & Nossas Dificuldades

Desenvolver um sistema robusto num ecossistema fechado não foi tão simples quanto parecia. Enfrentamos diversas batalhas e reescrevemos motores para alcançar os melhores resultados, em especial:

1. **O Desafio da Portabilidade Absoluta**
   Normalmente, sites e serviços web têm a Interface e o "Cérebro" rodando separados na nuvem. Nós tivemos que fazer o aplicativo visual `.exe` iniciar automaticamente um "servidor invisível fantasma" que hospeda o Banco de Dados e as rotas assim que você abre a Janela e, mais importante ainda, desligá-lo com segurança e varrer sua memória cache ao clicar no famoso 'X' para fechar. Graças a isso, conseguimos usar scripts Power-Shell de compliação (`ExportarPendrive.ps1`) que empacotam o `.NET Runtime` nativo sem depender que o PC do cliente o tenha instalado.

2. **As "Imagens Fantasmas" (O Paradoxo DuckDuckGo)**
   A princípio, nosso sistema tentava adivinhar visualmente os produtos sem imagem buscando o nome aleatoriamente na internet via Scraping. Ocorreu que buscas genéricas como *"Ovo"* retornavam praias, lugares e itens fora do escopo do usuário em pleno PDV. Refizemos a lógica de ponta a ponta construindo uma escala de 5 prioridades rígidas, ensinando a API a ler estritamente Bases de Código de Barras (EANs), garantindo assertividade quase 100%.

3. **Recibos SMTP e Autenticação Cega do Google**
   Projetamos um botão final de "Checkout" que dispara instantaneamente um lindo recibo via e-mail formatado (HTML). A dificuldade foi orquestrar essa segurança: um email automatizado requer liberação da Google, logo o código não poderia ter as senhas reais 'hardcoded' no repositório. Criamos a classe blindada `EmailSenderService` com blocos de Resiliência `Try/Catch` que, caso caia a internet, não trava mais a tela do caixa para o cliente na vida real, além de extrair de forma madura a leitura de Senha do arquivo JSON gerado apenas pela máquina.

4. **Tradução Lógica vs Visual**
   Realizamos a conversão massiva do vocabulário do Dark Theme e de variáveis inglesas/secas para português e português corporativo ("Inventário", "Relatórios", "Configurações"). Durante as etapas, esbarramos no erro em que as requisições API falhavam momentaneamente ao traduzirmos até os modelos computacionais em C# (colocando acentos `Ç` em propriedades sistêmicas globais da serialização). Compreendemos e separamos o modelo lógico (Back: *Preco*) da visão humana (Front: *R$ Preço*), ajustando definitivamente o ciclo do "Carrinho Esvaziado".

<br>

---

### **Agradecimento**
O projeto tomou de fato uma vida rica, uma interface de alta responsividade inspirada em plataformas empresariais padrão-ouro, operando com total integridade off-lines e on-line. Um passo em direção à gestão de negócios que simplesmente *funciona* fora da caixa!
