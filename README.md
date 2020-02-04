# 👉 Finger Sports

### Le jeu

Finger Sports est un jeu PC où deux joueurs s'affrontent dans différents mini jeux de sports, avec une particularité : Ils ne jouent pas avec une manette ou un clavier, mais avec leurs doigts. À la fin du timer, le joueur avec le plus de points gagne la partie. Les deux jeux proposés sont les sports de :

* 🏀 Basketball 
* ⚽ Football

Pour ça, il suffit simplement d'une webcam, ainsi que de deux papiers de couleurs vives (et distinctes l'une de l'autre) enroulés autour de leur doigt (les post-it ont l'avantage d'être de la bonne taille et d'être autocollants, sinon utilisez ceux fournis plus bas dans ce *README* à découper).

##### Basketball

![Basketball](/Images/Basketball.jpg)

##### Football

![Football](/Images/Football.jpg)

Un menu de paramètres vous permet simplement d'adapter les valeurs utilisées pour la détection, afin qu'elle prenne en compte les couleurs de vos propres papiers et les différences d'éclairages d'un environnement à l'autre. Il est conseillé d'utiliser des couleurs "fluo" et qui se distinguent de la peau et des vêtements portés par les joueurs. Par défaut, les couleurs utilisées sont le **rouge** et le **bleu**.

![Papiers](/Images/Papiers.jpg)

### Notre approche

Nous utilisons [Emgu CV](http://www.emgu.com/wiki/index.php/Main_Page) qui est un wrapper cross platform .Net pour la bibliothèque de traitement d'image **OpenCV**. Cela nous permet de récupérer le flux vidéo de la webcam et à partir de là d'appliquer plusieurs métodes pour détecter le déplacement et la rotation des mains des deux joueurs.

#### Étapes par étapes

À chaque capture, nous convertissons l'image dans l'espace de couleurs **HSV**, nous appliquons un flou Gaussien, et à partir de cette nouvelle image nous en créons deux (une pour chaque joueur) que l'on transforme en image binaire (noir et blanc) à partir des seuils configurés dans les paramètres sauvegardés.

Afin d'améliorer le résultat nous appliquons une érosion et une dilatation sur chacune des images, et nous détecter le contour de la plus grande forme (qui, si tout c'est bien passé, correspond au papier autour du doigt du joueur).

On cherche le rectangle qui englobe au mieux ce contour, et grâce à ce rectangle, on peut récupérer sa rotation et sa position.

Par contre, nous n'appliquons pas directement sa position au mains des joueurs dans le jeu, nous calculons le déplacement entre plusieurs frames pour le retranscrire sous forme d'un delta dans le jeu.
