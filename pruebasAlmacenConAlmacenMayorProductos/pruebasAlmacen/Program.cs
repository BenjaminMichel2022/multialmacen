using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace pruebasAlmacen
{
    // objetos para guardar la informacion
    //almacenes
    public class ProductoAlmacen
    {
        public string Almacen { get; set; }
        public int Cantidad { get; set; }
        public string Producto { get; set; }
    }
    //productos y la cantidad que piden
    public class ProductoCantidad
    {
        public string Descripcion { get; set; }
        public int Cantidad { get; set; }
    }
    //objeto para crea la respuesta final
    public class ProductoEnvio
    {
        public string Producto { get; set; }
        public int CantidadAlmacen { get; set; }
        public string Almacen { get; set; }
        public int CantidadEnvio { get; set; }
        public string Observacion { get; set; }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            //hora de inicio
            string date = DateTime.Now.ToString("h:mm:ss:fff");
            Console.WriteLine("Inicio {0}", date);
            //se crean las listas para recorrer los almacenes
            //la lista de "todos" entran los que por lo menos un almacen cumple para mandar en un solo envio
            List<List<ProductoAlmacen>> todos = new List<List<ProductoAlmacen>>();
            //la lista de todosNoCompleto entran los que ningun almacen lo tiene para un solo envio pero la suma de unidades si lo completa
            List<List<ProductoAlmacen>> todosNoCompleto = new List<List<ProductoAlmacen>>();
            // la lista envios se guarda de donde lo va mandar y de ella mostrar la respuesta en pantalla
            List<ProductoEnvio> envios = new List<ProductoEnvio>();

            //se carga la informacion de los productos que compra el cliente y la cantidad
            string ubicacionArchivoProductos = "C:\\Users\\sistemas14\\Desktop\\productos\\pruebaProductos2.csv";
            StreamReader archivoProductos = new StreamReader(ubicacionArchivoProductos);

            string lineaProductos;
            //la lista cantidad guarda todos los productos y la cantidad necesaria
            List<ProductoCantidad> cantidad = new List<ProductoCantidad>();
            //arreglo para trabajar con la informacion del csv
            string[] productos;
            // se empieza a guardar en la lista cantidad todos los productos del csv
            while ((lineaProductos = archivoProductos.ReadLine()) != null)
            {
                if (lineaProductos != "")
                {
                    //se quitan los caracteres no necesarios creo que aqui se podria trabajar con el json
                    lineaProductos = lineaProductos.Replace("\"", string.Empty).Replace("[", string.Empty).Replace("]", string.Empty).Replace("'", string.Empty);
                    productos = lineaProductos.Split(',');
                    //se almacena en la lista cantidad
                    cantidad.Add(new ProductoCantidad { Descripcion = productos[0], Cantidad = Int32.Parse(productos[1]) });
                }
            }
            //se carga la informacion de los almacenes con la cantidad del producto consultado
            //lo ideal seria que de inicio dijera que producto es por cuestiones de pruebas se asigna el producto que es
            string ubicacionArchivoAlmacenes = "C:\\Users\\sistemas14\\Desktop\\almacenes\\pruebaAlmacen2.csv";
            StreamReader archivoAlmacenes = new StreamReader(ubicacionArchivoAlmacenes);

            string lineaAlmacenes;
            //lista para ver el top, ver si por lo menos uno cumple con la cantidad que se requiere enviar y si la suma de lo que se envia cumple con lo que se pide
            List<ProductoAlmacen> almacenProducto = new List<ProductoAlmacen>();
            //arreglos para trabajar con la informacion del csv
            string[] almacenes;
            string[] almacenProductoSeparado;
            //arreglo que obtiene de cada producto el almacen que tiene mas cantidad
            string[] top = new string[cantidad.Count];
            //la variable cantidadPosicion indica la pocicion para agregar el producto segun como se valla recorriendo el csv
            int cantidadPosision = 0;
            //inicia la lectura de los almacenes con los productos que hay en cada uno
            while ((lineaAlmacenes = archivoAlmacenes.ReadLine()) != null)
            {
                if (lineaAlmacenes != "")
                {
                    //lee la linea y quita los caracteres innecesarios 
                    almacenes = Regex.Split(lineaAlmacenes.Replace("\"", string.Empty).Replace(",", string.Empty).TrimEnd(']').TrimStart('[').Replace("' ", ",").Replace("'", string.Empty), @"\]\[");
                    foreach (var lista in almacenes)
                    {
                        almacenProductoSeparado = lista.Split(',');
                        //almacena los almacenes con la cantidad que tiene cada uno
                        almacenProducto.Add(new ProductoAlmacen { Almacen = almacenProductoSeparado[0], Cantidad = Int32.Parse(almacenProductoSeparado[1]), Producto = cantidad[cantidadPosision].Descripcion });
                    }
                    //ordena los almacenes de menor a mayor segun la cantidad de producto que tenga
                    almacenProducto.Sort(delegate (ProductoAlmacen x, ProductoAlmacen y)
                    {
                        return x.Cantidad.CompareTo(y.Cantidad);
                    });
                    //invierte la lista para que quede ordenado de mayor a menor
                    almacenProducto.Reverse();
                    // se obtiene el primer almacen que tenga mas producto
                    top[cantidadPosision] = almacenProducto[0].Almacen;
                    //obtiene los almacenes donde se pueda enviar de un solo envio utilizando linq
                    var resultado = from prot in almacenProducto
                                    where prot.Cantidad >= cantidad[cantidadPosision].Cantidad
                                    select prot;
                    //obtiene la suma de lo que contiene cada almacen del producto que se esta analizando con linq
                    var sumaResultado = almacenProducto.Sum(item => item.Cantidad);


                    //rebisa que si no se completa de un solo envio y si la suma completa
                    if (resultado.Count() == 0 && sumaResultado > cantidad[cantidadPosision].Cantidad)
                    {
                        //en caso de cumplir se guarda para fraccionar el pedido
                        todosNoCompleto.Add(new List<ProductoAlmacen>(almacenProducto));
                    }
                    // en caso de que no se complete el pedido de algun producto entre todos los almacenes por razones de ovnis muestra un mensaje con el producto que no se completo
                    //ya sea que terminemos el proceso para ajustar la cantidad del producto o avizar que se tenga que generar una nueva orden deventa para pedir los existentes segun se requiera
                    else if (sumaResultado < cantidad[cantidadPosision].Cantidad)
                    {
                        //mensaje
                        Console.WriteLine("El producto {0} no se completa por lo que se pide revizar este producto ya que no se mandara pero lo demas si", cantidad[cantidadPosision].Descripcion);
                    }
                    else //en caso de que si exista por lo menos uno que se pueda enviar de uno solo se guarda en la lista de todos
                    {
                        //solo guarda los elementos que cumplen con la cantidad del producto en un solo envio
                        todos.Add(resultado.ToList<ProductoAlmacen>());
                    }
                    //limpia la lista del almacenes ya analisados y seguir con los almacenes del siguiente producto
                    almacenProducto.Clear();

                    cantidadPosision++;

                }


            }
            //obtiene el almacen con mas productos en cantidad
            var topGrupo = top.GroupBy(x => x);// primero los agrupa y revisa cuantas veces se repite

            var topLargest = topGrupo.OrderByDescending(x => x.Count()).First(); //los ordena de mayor a menor segun las veces que se repitio y obtiene el primero

            string almacenPrimario = topLargest.Key; //se guarda en la variable almacenPrimario el almacen que tubo mas cantidad entre los productos que se pidieron

            Console.Write("El {0} es el que mas producto tiene", almacenPrimario);//para pruebas lo muestra en pantalla

            //se declara la bandera para saber si va entrar en un almacen diferente al que se definio como primario (Top)
            var banderaSecundaria = false;
            //las siguientes variables fueron creadas para llevar el control de los almacenes usados anteriormente
            var almacenCompuesto = "";
            var almacenSecundario = "";
            var almaceneUnico = "";
            //bandera para ver si un almacen fue usado anteriormente
            var banderaUsadoSecundario = false;
            //indica el almacen que se estanalisando al iniciar en 0 prioriza el que tiene mas
            var indice = 0;
            //Inicia el proceso para los almacenes que completan los envios de una sola ves
            foreach (var list in todos)//recorre el conjunto de almacenes
            {
            restart1: //etiqueta para reiniciar en caso de que no cumpla alguna condicion
                //inicio de la cantidad 
                foreach (var element in list)//recorre cada almacen
                {
                    //almacenInicial guarda el almacen que se va analizar
                    var almacenInicial = list[indice];
                    //obtiene la cantidad que se requiere enviar con linq en este caso se hace por descripcion del producto lo ideal seria hacerlo por id
                    var CantidadEnvio = from env in cantidad
                                        where env.Descripcion == element.Producto
                                        select env;
                    //si el producto a enviar de un solo envio se puede mandar desde mas de un almacen o el almacen del que se envia es unico pero es el almacen que se tiene como top
                    //entra al siguiente if ya que si es unico y es diferente al almacen con mas producto(top) se requiere guardar de cual se mando para usarlo despues en caso de ser requerido
                    if (list.Count > 1 || almacenPrimario.Equals(element.Almacen))
                    {
                        //el primer almacen que se analisa es el top ya que es el que priorisa
                        if (almacenInicial.Almacen.Equals(element.Almacen) && almacenPrimario.Equals(element.Almacen))
                        {
                            // guarda la informacion del envio 
                            // los datos necesarios a mostrar en pantalla fueron el almacen, el producto y cantidadEnvio;
                            // los demas datos solo fueron para realizar pruebas y ver que la cantidadAlmacen cumpliera con el envio
                            // la observacion solo fue para pruebas y ver por que se agregaba al listado de envios
                            envios.Add(new ProductoEnvio
                            {
                                Almacen = element.Almacen,
                                Producto = element.Producto,
                                CantidadAlmacen = element.Cantidad,
                                CantidadEnvio = CantidadEnvio.ToList()[0].Cantidad,
                                Observacion = "Almacen top de existencias para un solo envio"
                            });
                            //ya que encontro el almacen y va pasar a otro conjunto de almacenes reinicia el indice
                            indice = 0;
                            break;
                        }
                        //en caso de que el almacen top no cumpla con el envio de una sola vez se revisa si anteriormente se utilizo algun almacen anteriormente.
                        if (almacenInicial.Almacen.Equals(element.Almacen) && (almacenSecundario.Contains(element.Almacen) || almaceneUnico.Contains(element.Almacen)) && banderaSecundaria && almacenSecundario.Contains(element.Almacen) && banderaUsadoSecundario)
                        {
                            // guarda la informacion del envio 
                            // los datos necesarios a mostrar en pantalla fueron el almacen, el producto y cantidadEnvio;
                            // los demas datos solo fueron para realizar pruebas y ver que la cantidadAlmacen cumpliera con el envio
                            // la observacion solo fue para pruebas y ver por que se agregaba al listado de envios
                            envios.Add(new ProductoEnvio
                            {
                                Almacen = element.Almacen,
                                Producto = element.Producto,
                                CantidadAlmacen = element.Cantidad,
                                CantidadEnvio = CantidadEnvio.ToList()[0].Cantidad,
                                Observacion = "Almacen con existencia para un solo envio usado anteriormente"
                            });
                            //almacena el almacen usado pero solo si no se ha utilizado antes
                            //nota ya que se pudo almacenar en el almacen de envio unico
                            if (!almacenSecundario.Contains(element.Almacen))
                                almacenSecundario += ", " + element.Almacen;
                            //ya que encontro el almacen y va pasar a otro conjunto de almacenes reinicia el indice
                            indice = 0;
                            //y bandera secundaria pas a falso para que no entre
                            banderaSecundaria = false;
                            break;
                        }//en caso de que los almacenes que cumplen en un solo envio no se hayan utilizado anteriormente y no sea el top 
                        //se debe tomar en cuenta el primer almacen con mayor producto que cumpla
                        //y sin importar si se utiliso antes enviarlo para que el producto llegue de una sola pieza
                        //se debe almacenar el almacen usado para utilizarlo despues
                        else if (almacenInicial.Almacen.Equals(element.Almacen) && !almacenPrimario.Equals(element.Almacen) && banderaSecundaria && !banderaUsadoSecundario)
                        {
                            // guarda la informacion del envio 
                            // los datos necesarios a mostrar en pantalla fueron el almacen, el producto y cantidadEnvio;
                            // los demas datos solo fueron para realizar pruebas y ver que la cantidadAlmacen cumpliera con el envio
                            // la observacion solo fue para pruebas y ver por que se agregaba al listado de envios
                            envios.Add(new ProductoEnvio
                            {
                                Almacen = element.Almacen,
                                Producto = element.Producto,
                                CantidadAlmacen = element.Cantidad,
                                CantidadEnvio = CantidadEnvio.ToList()[0].Cantidad,
                                Observacion = "Almacen con existencia para un solo envio"
                            });
                            //almacena el almacen usado pero solo si no se ha utilizado antes
                            if (!almacenSecundario.Contains(element.Almacen))
                                almacenSecundario += ", " + element.Almacen;
                            //ya que encontro el almacen y va pasar a otro conjunto de almacenes reinicia el indice
                            indice = 0;
                            //ya que ahora existe un almacen usado diferente al top se debe desabilitar bandera secundaria para que de inicio no entre si no cumple con el top
                            banderaSecundaria = false;
                            //pero se debe decir que ya hay elementos usados anteriormente
                            banderaUsadoSecundario = true;
                            break;
                        }
                        // En caso de no se haya utilizado antes y se este llegando al final del ciclo se debe desactivar la bandera de los almacenes usados anteriormente
                        if (element.Almacen.Equals(list[list.Count() - 1].Almacen) && banderaUsadoSecundario && banderaSecundaria)
                        {
                            banderaUsadoSecundario = false;//se desactiva la bandera de almacenes usados anteriormente
                            indice = 0;//ya que no se cumplio ninguna condicion se reinicia el indice
                            goto restart1;//reinicio del foreach del conjunto que analiza actualmente
                        }
                        //en caso de que el almacen top no cumpla se debe analizar los almacenes que si cumplen diferentes al top
                        if (element.Almacen.Equals(list[list.Count() - 1].Almacen))
                        {
                            banderaSecundaria = true;// se activa la bandera de almacenes diferentes al tob
                            indice = 0;//ya que no se cumplio ninguna condicion se reinicia el indice
                            goto restart1;//reinicio del foreach del conjunto que analiza actualmente
                        }

                    }
                    else // almacen que completa pero solo un almacen tiene para un envio completo y es diferente al top
                    {
                        // guarda la informacion del envio 
                        // los datos necesarios a mostrar en pantalla fueron el almacen, el producto y cantidadEnvio;
                        // los demas datos solo fueron para realizar pruebas y ver que la cantidadAlmacen cumpliera con el envio
                        // la observacion solo fue para pruebas y ver por que se agregaba al listado de envios
                        envios.Add(new ProductoEnvio
                        {
                            Almacen = element.Almacen,
                            Producto = element.Producto,
                            CantidadAlmacen = element.Cantidad,
                            CantidadEnvio = CantidadEnvio.ToList()[0].Cantidad,
                            Observacion = "Almacen con existencia unica del producto para un solo envio"
                        });
                        //almacena el almacen usado pero solo si no se ha utilizado antes
                        if (!almaceneUnico.Contains(element.Almacen))
                            almaceneUnico += ", " + element.Almacen;
                        //ya que aqui se esta utilizando un almacen diferente al top se guarda para utilizarlo despues ademas de activar la bandera  para ver si es usado el almacen antes
                        banderaUsadoSecundario = true;
                        break;
                    }
                    //en caso de algun fallo y que por alguna razon el producto no se halla enviado se puede utilizar el siguiente if para marcar algun error en esta seccion
                    if (indice == list.Count() - 1)
                        indice = 0;
                    else //el else es para cuando no se cumpla de que sea de un solo envio o que sea el top y pase a analizar el siguiente almacen
                        indice++;
                }
            }
            //ya que completo los envios completados en un solo envio se dispone a enviar los envios fraccionados
            if (todosNoCompleto.Count() > 0)
            {
                foreach (var producto in cantidad) //para llevar un orden y obtener el total del envio se recorren todos los productos 
                {
                    var sumaTotal = 0;//variable para llevar el conteo de lo que se a mandado
                    var almacenP = true; // bandera para iniciar con el almacen con mayor producto (top)
                    var banderaCicloUnico = false; // bandera para analisar los almacenes que tubieron envios y completaron de un solo envio los productos
                    var pasosCicloUnico = 1; //ya que se utilizan dos variables para almacenar los usados anteriormente se reviza cuantas veces debe analizar si se utilizo anteriormente
                                             //solo debe pasar dependiendo la cantidad de difentes almacenes utilizados anteriormente
                                             //talvez lo obtimo seria utilizar una sola
                    var usadoAnteriormente = true; //bandera para revizar los usados anteriormente pero no fueron utilizados en la lista de un solo envio

                    foreach (var list in todosNoCompleto) //inicio de envios de productos que se envian por partes de diferentes almacenes
                    {
                    restart://etiqueta para reiniciar el analicis del almacen si llega al final y no cumple con ninguna bandera
                        foreach (var element in list)//se asigna el almacen con productos para la suma
                        {
                            if (producto.Descripcion.Equals(element.Producto)) //se revisa si el producto que se esta analizando corresponde al del producto del que se requiere fraccionar
                            {
                                //de primera instacia se mandan los del almacen top
                                if (element.Almacen.Equals(almacenPrimario) && almacenP)
                                {
                                    sumaTotal += element.Cantidad;//ya que se requiere fraccionar el envio se lleva el control de cuantos se han enviado hasta el momento
                                    
                                    // los datos necesarios a mostrar en pantalla fueron el almacen, el producto y cantidadEnvio;
                                    // los demas datos solo fueron para realizar pruebas y ver que la cantidadAlmacen cumpliera con el envio
                                    // la observacion solo fue para pruebas y ver por que se agregaba al listado de envios
                                    envios.Add(new ProductoEnvio
                                    {
                                        Almacen = element.Almacen,
                                        Producto = element.Producto,
                                        CantidadAlmacen = element.Cantidad,
                                        //en la cantidad de envio se valida si supero o igualo la cantidad de envio total.
                                        //en caso de superarlo se mandara la cantidad que tomo para completar el pedido
                                        //en caso de no superarlo mandara el total que cuenta el almacen.
                                        CantidadEnvio = sumaTotal <= producto.Cantidad ? element.Cantidad : element.Cantidad - (sumaTotal - producto.Cantidad),
                                        Observacion = "Almacen asignado al cliente con una parte del total del producto"
                                    });

                                    //en caso de superar el producto a enviar debe finalizar y pasar al siguiente producto
                                    if (sumaTotal >= producto.Cantidad)
                                    {
                                        break;
                                    }
                                    //en caso contrario se debe decir que ya no es necesario entrar a esta condicion de bandera
                                    almacenP = false;
                                    //se reinicia el foreach para seguir fraccionando el envio del producto
                                    goto restart;

                                }
                                // almacen usados en la primera lista de todos los almacenes usados en los de un solo envio
                                //para lo que se analisa que el total no se haya completado, que se contenga en las variables  de almacenSecundario y almacenUnico
                                //que sean diferentes de vacio, que ya se haya analizado el almacen top y que no le toque el turno a los almacenes usados anteriormente pero para fraccionar pedidos anteriores
                                if (sumaTotal < producto.Cantidad && (almaceneUnico.Contains(element.Almacen) || almacenSecundario.Contains(element.Almacen)) && (!almaceneUnico.Equals("") || !almacenSecundario.Equals("")) && !almacenP && !banderaCicloUnico)
                                {
                                    sumaTotal += element.Cantidad;//ya que se requiere fraccionar el envio se lleva el control de cuantos se han enviado hasta el momento
                                    // los datos necesarios a mostrar en pantalla fueron el almacen, el producto y cantidadEnvio;
                                    // los demas datos solo fueron para realizar pruebas y ver que la cantidadAlmacen cumpliera con el envio
                                    // la observacion solo fue para pruebas y ver por que se agregaba al listado de envios
                                    envios.Add(new ProductoEnvio
                                    {
                                        Almacen = element.Almacen,
                                        Producto = element.Producto,
                                        CantidadAlmacen = element.Cantidad,
                                        CantidadEnvio = sumaTotal <= producto.Cantidad ? element.Cantidad : element.Cantidad - (sumaTotal - producto.Cantidad),
                                        Observacion = "Almacen con envios unicos o secundarios anteriormente con una parte del total del producto"
                                    });
                                    //hay que revisar la cantidad de almacenes que existen utilisados anteriormente ya que esa es la cantidad de veces que debe entrar a esta condicion
                                    var cantidadAlmacen = almaceneUnico.Split(',');//se convierte en lista
                                    var cantidadAlmacenSec = almacenSecundario.Split(',');//se convierte en lista
                                    var cantidadUnion = cantidadAlmacen.Union(cantidadAlmacenSec);// se genera la union entre ambas listas para mostrar solo los distintos

                                    pasosCicloUnico++;//se cuenta las veces que se ha pasdo por esta opcion

                                    //en caso de superar el producto a enviar debe finalizar y pasar al siguiente producto
                                    if (sumaTotal >= producto.Cantidad)
                                    {
                                        break;
                                    }
                                    //en caso de no superar el producto a enviar debe analisar si es el ultimo elemento de los almacenes o en su caso que ya haya visto que analiso todos
                                    //los almacenes utilizados anteriormente en la lista de envios completos de un solo envio
                                    else if (element.Almacen.Equals(list[list.Count() - 1].Almacen) || cantidadUnion.Count() == pasosCicloUnico)
                                    {
                                        banderaCicloUnico = true;//activa la bandera para empesar a analisar los demas almacenes
                                        goto restart;// se reinicia el foreach para seguir fraccionando el envio del producto
                                    }

                                }
                                //en caso de que no existan elementos enviados de otros almacenes no tiene caso seguir analizando y se cambia la bandera 
                                //para seguir analizando los demas almacenes
                                else if (almaceneUnico.Equals("") && almacenSecundario.Equals("") && !almacenP && !banderaCicloUnico)
                                {
                                    banderaCicloUnico = true;//activa la bandera para empesar a analisar los demas almacenes
                                    goto restart;// se reinicia el foreach para seguir fraccionando el envio del producto
                                }
                                //revisa si no se completo y manda de los otros almacenes que no se han utilizado anteriormente
                                if (sumaTotal < producto.Cantidad && !almaceneUnico.Contains(element.Almacen) && !almacenSecundario.Contains(element.Almacen) && !almacenP && banderaCicloUnico)
                                {
                                    //solo entra una ves cuando empieza a usar este tipo de almacen para generar el primer registro de almacenes para ver si no se han utilizado entes
                                    if (almacenCompuesto.Equals("") && !almacenPrimario.Equals(element.Almacen))
                                    {
                                        sumaTotal += element.Cantidad; 
                                        envios.Add(new ProductoEnvio
                                        {
                                            Almacen = element.Almacen,
                                            Producto = element.Producto,
                                            CantidadAlmacen = element.Cantidad,
                                            CantidadEnvio = sumaTotal <= producto.Cantidad ? element.Cantidad : element.Cantidad - (sumaTotal - producto.Cantidad),
                                            Observacion = "Almacen con existencia del producto pero solo una parte ultima opcion primer envio"
                                        });

                                        almacenCompuesto = element.Almacen;


                                    }
                                    else //revisa si se manda de un almacen usado anteriormente en la condicion anterior y la siguiente
                                    if (almacenCompuesto.Contains(element.Almacen) && usadoAnteriormente && !almacenPrimario.Equals(element.Almacen))
                                    {
                                        sumaTotal += element.Cantidad;
                                        envios.Add(new ProductoEnvio
                                        {
                                            Almacen = element.Almacen,
                                            Producto = element.Producto,
                                            CantidadAlmacen = element.Cantidad,
                                            CantidadEnvio = sumaTotal <= producto.Cantidad ? element.Cantidad : element.Cantidad - (sumaTotal - producto.Cantidad),
                                            Observacion = "Almacen con existencia producto pero solo una parte usado anteriormente"
                                        });

                                    }
                                    //ya si de los usados anteriormente no se completa empieza usar de los otros que no se han utilisado
                                    else if (!almacenCompuesto.Contains(element.Almacen) && !usadoAnteriormente && !almacenPrimario.Equals(element.Almacen))
                                    {
                                        almacenCompuesto += ", " + element.Almacen;
                                        sumaTotal += element.Cantidad;
                                        envios.Add(new ProductoEnvio
                                        {
                                            Almacen = element.Almacen,
                                            Producto = element.Producto,
                                            CantidadAlmacen = element.Cantidad,
                                            CantidadEnvio = sumaTotal <= producto.Cantidad ? element.Cantidad : element.Cantidad - (sumaTotal - producto.Cantidad),
                                            Observacion = "Almacen con existencia producto pero solo una parte ultimas opciones"
                                        });
                                    }

                                    //en caso de superar el producto a enviar debe finalizar y pasar al siguiente producto
                                    if (sumaTotal >= producto.Cantidad)
                                    {
                                        break;
                                    }
                                    //en caso de que se llegue al final y no se haya completado con los usados anteriormente debe seguir con los que no se han utilizado
                                    else if (element.Almacen.Equals(list[list.Count() - 1].Almacen))
                                    {
                                        usadoAnteriormente = false;
                                        goto restart;
                                    }
                                }
                                //en caso de que se llegue al final y no se haya completado con los usados anteriormente debe seguir con los que no se han utilizado
                                if (sumaTotal <= producto.Cantidad && element.Almacen.Equals(list[list.Count() - 1].Almacen) && usadoAnteriormente)
                                {
                                    usadoAnteriormente = false;
                                    goto restart;
                                }
                            }
                        }
                    }
                }
            }
            //una ves terminado en este punto se puede mandar el json para guardar la informacion de los envios 
            //la variable caja es lo que se imprimira en pantalla
            string caja = "";
            //ya que para mejor visibilidad se ve mejor cuando solo muestra el producto y enseguida el almacen y cuanto envia 
            //se crea la siguiente variable para ver que sea igual al anterior
            var productoAnterior = ""; 

            foreach (var item in envios)//inicio de llenado de caja para imprimir en pantalla
            {
                if (productoAnterior.Equals(item.Producto))//en caso de ser igual solo agrega el almcen y la cantidad que envia
                {
                    caja += string.Format(" {0}, {1};", item.Almacen, item.CantidadEnvio);
                }
                else// caso contrario agrega el producto el almacen y la cantidad que envia
                {
                    caja += string.Format("\n\n {0}, {1}, {2};", item.Producto, item.Almacen, item.CantidadEnvio);
                }


                productoAnterior = item.Producto;
            }
            Console.WriteLine(caja); //imprime en pantalla

            date = DateTime.Now.ToString("h:mm:ss:fff");//hora que finalizo
            Console.WriteLine("\n\nFin {0}", date);
            Console.ReadLine();
        }
    }
}
