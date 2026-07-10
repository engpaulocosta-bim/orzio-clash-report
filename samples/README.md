Os fixtures `sample-clash.xml` e `sample-clash2.xml` são exports do Navisworks Clash
Detective com dados sintéticos/anonimizados (nomes de projeto, empresa, caminhos de rede e
nomes de ficheiro foram substituídos por valores fictícios). A estrutura do XML, contagem de
elementos e relações necessárias para parsing/grouping foram preservadas.

Se for adicionar um novo fixture a partir de um export real, anonimize nomes de cliente,
empresa, projeto, caminhos absolutos, letras de rede e nomes de ficheiro reais antes de
versionar o ficheiro.

`run-manifest.sample.json` é um fixture separado e totalmente sintético: declara manualmente
três modelos/revisões de uma rodada de coordenação hipotética. O manifesto é uma declaração
explícita — a revisão de cada modelo nunca é inferida a partir do nome do arquivo, do
caminho ou de qualquer XML. Ele ainda não está vinculado aos fixtures `sample-clash*.xml`
nem à CLI.
