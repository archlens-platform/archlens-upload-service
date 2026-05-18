# language: pt-BR
Funcionalidade: Exclusão de Diagrama
  Como um usuário do sistema
  Eu quero excluir diagramas
  Para remover dados que não são mais necessários

  Cenário: Excluir diagrama existente
    Dado que existe um diagrama cadastrado no sistema
    Quando eu envio uma requisição DELETE para o diagrama existente
    Então a resposta deve ter status code 204

  Cenário: Excluir diagrama inexistente
    Quando eu envio uma requisição DELETE para "/diagrams/00000000-0000-0000-0000-000000000099"
    Então a resposta deve ter status code 404

  Cenário: Diagrama não é mais encontrado após exclusão
    Dado que existe um diagrama cadastrado no sistema
    E que o diagrama foi excluído
    Quando eu envio uma requisição GET para o diagrama existente
    Então a resposta deve ter status code 404
