# language: pt-BR
Funcionalidade: Upload de Diagrama
  Como um usuário do sistema
  Eu quero fazer upload de diagramas de arquitetura
  Para que eles sejam processados e analisados

  Cenário: Upload de arquivo PNG válido
    Dado que eu tenho um arquivo PNG válido chamado "diagram.png"
    Quando eu envio o upload do arquivo para "/diagrams"
    Então a resposta deve ter status code 201
    E a resposta deve conter o campo "diagramId"
    E a resposta deve conter o campo "status" com valor "Received"

  Cenário: Upload de arquivo JPG válido
    Dado que eu tenho um arquivo JPG válido chamado "architecture.jpg"
    Quando eu envio o upload do arquivo para "/diagrams"
    Então a resposta deve ter status code 201
    E a resposta deve conter o campo "fileName"

  Cenário: Upload de arquivo com extensão não suportada
    Dado que eu tenho um arquivo com extensão não suportada chamado "doc.txt"
    Quando eu envio o upload do arquivo para "/diagrams"
    Então a resposta deve ter status code 415

  Cenário: Upload de arquivo com assinatura inválida
    Dado que eu tenho um arquivo com assinatura inválida chamado "fake.png"
    Quando eu envio o upload do arquivo para "/diagrams"
    Então a resposta deve ter status code 415

  Cenário: Upload de arquivo duplicado retorna o existente
    Dado que eu tenho um arquivo PNG válido chamado "duplicate.png"
    E que eu já fiz upload desse arquivo anteriormente
    Quando eu envio o upload do arquivo para "/diagrams"
    Então a resposta deve ter status code 201
    E a resposta deve conter o campo "isDuplicate" com valor "True"
