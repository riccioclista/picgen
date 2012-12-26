

for i in $(ls input/ | grep cfdg) ; do cfdg -s 500 input/$i examples/${i}.png; done

i=1024; while [ $i -gt 15 ]; do echo $i ; i=$(($i/2)); convert examples/MatrixRender.png -resize $i examples/MatrixRender-${i}.png ; done

