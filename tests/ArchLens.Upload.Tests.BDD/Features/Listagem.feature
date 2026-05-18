# language: pt-BR
Funcionalidade: Listagem de Diagramas
  Como um usuário do sistema
  Eu quero listar e consultar diagramas
  Para acompanhar o status dos meus uploads

  Cenário: Listar diagramas quando existem registros
    Dado que existem 3 diagramas cadastrados no sistema
    Quando eu envio uma requisição GET para "/diagrams"
    Então a resposta deve ter status code 200
    E a resposta deve conter o campo "items"
    E a resposta deve conter o campo "totalCount"

  Cenário: Listar diagramas quando não existem registros
    Quando eu envio uma requisição GET para "/diagrams"
    Então a resposta deve ter status code 200

  Cenário: Obter diagrama por ID existente
    Dado que existe um diagrama cadastrado no sistema
    Quando eu envio uma requisição GET para o diagrama existente
    Então a resposta deve ter status code 200
    E a resposta deve conter o campo "diagramId"
    E a resposta deve conter o campo "fileName"
    E a resposta deve conter o campo "status"

  Cenário: Obter diagrama por ID inexistente
    Quando eu envio uma requisição GET para "/diagrams/00000000-0000-0000-0000-000000000099"
    Então a resposta deve ter status code 404

  Cenário: Obter status de diagrama existente
    Dado que existe um diagrama cadastrado no sistema
    Quando eu envio uma requisição GET para o status do diagrama existente
    Então a resposta deve ter status code 200
    E a resposta deve conter o campo "status"

  Cenário: Obter status de diagrama inexistente
    Quando eu envio uma requisição GET para "/diagrams/00000000-0000-0000-0000-000000000099/status"
    Então a resposta deve ter status code 404

  Cenário: Listar diagramas com paginação
    Dado que existem 5 diagramas cadastrados no sistema
    Quando eu envio uma requisição GET para "/diagrams?page=1&pageSize=2"
    Então a resposta deve ter status code 200
    E a resposta deve conter o campo "totalCount"
