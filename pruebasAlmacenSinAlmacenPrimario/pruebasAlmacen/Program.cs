using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace pruebasAlmacen
{

    public class ProductoAlmacen
    {
        public string Almacen { get; set; }
        public int Cantidad { get; set; }
        public string Producto { get; set; }
    }
    public class ProductoCantidad
    {
        public string Descripcion { get; set; }
        public int Cantidad { get; set; }
    }

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
            string date = DateTime.Now.ToString("h:mm:ss:fff");
            Console.WriteLine("Inicio {0}", date);
            List<List<ProductoAlmacen>> todos = new List<List<ProductoAlmacen>>();
            List<List<ProductoAlmacen>> todosNoCompleto = new List<List<ProductoAlmacen>>();
            List<ProductoEnvio> envios = new List<ProductoEnvio>();

            string ubicacionArchivoProductos = "C:\\Users\\sistemas14\\Desktop\\productos\\pruebaProductos20.csv";
            StreamReader archivoProductos = new StreamReader(ubicacionArchivoProductos);

            string lineaProductos;
            List<ProductoCantidad> cantidad = new List<ProductoCantidad>();
            string[] productos;
            while ((lineaProductos = archivoProductos.ReadLine()) != null)
            {
                if (lineaProductos != "")
                {
                    lineaProductos = lineaProductos.Replace("\"", string.Empty).Replace("[", string.Empty).Replace("]", string.Empty).Replace("'", string.Empty);
                    productos = lineaProductos.Split(',');
                    cantidad.Add(new ProductoCantidad { Descripcion = productos[0], Cantidad = Int32.Parse(productos[1]) });
                }
            }

            string ubicacionArchivoAlmacenes = "C:\\Users\\sistemas14\\Desktop\\almacenes\\pruebaAlmacen20.csv";
            StreamReader archivoAlmacenes = new StreamReader(ubicacionArchivoAlmacenes);

            string lineaAlmacenes;
            List<ProductoAlmacen> almacenProducto = new List<ProductoAlmacen>();
            string[] almacenes;
            string[] almacenProductoSeparado;
            int cantidadPosision = 0;
            while ((lineaAlmacenes = archivoAlmacenes.ReadLine()) != null)
            {
                if (lineaAlmacenes != "")
                {

                    almacenes = Regex.Split(lineaAlmacenes.Replace("\"", string.Empty).Replace(",", string.Empty).TrimEnd(']').TrimStart('[').Replace("' ", ",").Replace("'", string.Empty), @"\]\[");
                    foreach (var lista in almacenes)
                    {
                        almacenProductoSeparado = lista.Split(',');
                        almacenProducto.Add(new ProductoAlmacen { Almacen = almacenProductoSeparado[0], Cantidad = Int32.Parse(almacenProductoSeparado[1]), Producto = cantidad[cantidadPosision].Descripcion });
                    }

                    almacenProducto.Sort(delegate (ProductoAlmacen x, ProductoAlmacen y)
                    {
                        return x.Cantidad.CompareTo(y.Cantidad);
                    });
                    almacenProducto.Reverse();


                    var resultado = from prot in almacenProducto
                                    where prot.Cantidad >= cantidad[cantidadPosision].Cantidad
                                    select prot;
                    var sumaResultado = almacenProducto.Sum(item => item.Cantidad);



                    if (resultado.Count() == 0 && sumaResultado > cantidad[cantidadPosision].Cantidad)
                    {
                        todosNoCompleto.Add(new List<ProductoAlmacen>(almacenProducto));
                    }
                    else if (sumaResultado < cantidad[cantidadPosision].Cantidad)
                    {
                        Console.WriteLine("El producto {0} no se completa por lo que se pide revizar este producto ya que no se mandara pero lo demas si", cantidad[cantidadPosision].Descripcion);
                    }
                    else
                    {
                        todos.Add(resultado.ToList<ProductoAlmacen>());
                    }
                    almacenProducto.Clear();

                    cantidadPosision++;

                }


            }


            var almacenCompuesto = "";
            var almacenSecundario = "";
            var almaceneUnico = "";
            var banderaUsadoSecundario = false;

            var contador = 0;
            var contadorPrimario = 0;
            var indice = 0;
            //inicio de almacenes con un solo envio
            foreach (var list in todos)
            {
            restart1:
                foreach (var element in list)
                {

                    var almacenInicial = list[indice];
                    var CantidadEnvio = from env in cantidad
                                        where env.Descripcion == element.Producto
                                        select env;
                    if (list.Count > 1)
                    {
                        
                        //Otros usado anteriormente
                        if (almacenInicial.Almacen.Equals(element.Almacen) && (almacenSecundario.Contains(element.Almacen) || almaceneUnico.Contains(element.Almacen)) && banderaUsadoSecundario)
                        {
                            envios.Add(new ProductoEnvio
                            {
                                Almacen = element.Almacen,
                                Producto = element.Producto,
                                CantidadAlmacen = element.Cantidad,
                                CantidadEnvio = CantidadEnvio.ToList()[0].Cantidad,
                                Observacion = "Almacen con existencia para un solo envio usado anteriormente"
                            });
                            contador++;
                            if (!almacenSecundario.Contains(element.Almacen))
                                almacenSecundario += ", " + element.Almacen;
                            indice = 0;
                            break;
                        }//Otros sin usar anteriormente
                        else if (almacenInicial.Almacen.Equals(element.Almacen) && !banderaUsadoSecundario)
                        {
                            envios.Add(new ProductoEnvio
                            {
                                Almacen = element.Almacen,
                                Producto = element.Producto,
                                CantidadAlmacen = element.Cantidad,
                                CantidadEnvio = CantidadEnvio.ToList()[0].Cantidad,
                                Observacion = "Almacen con existencia para un solo envio"
                            });
                            contador++;
                            if (!almacenSecundario.Contains(element.Almacen))
                                almacenSecundario += ", " + element.Almacen;
                            indice = 0;
                            banderaUsadoSecundario = true;
                            break;
                        }
                        if (element.Almacen.Equals(list[list.Count() - 1].Almacen) && banderaUsadoSecundario)
                        {
                            banderaUsadoSecundario = false;
                            indice = 0;
                            goto restart1;
                        }

                        if (element.Almacen.Equals(list[list.Count() - 1].Almacen))
                        {
                            indice = 0;
                            goto restart1;
                        }

                    }
                    else // almacen que completa pero solo un almacen tiene para un envio
                    {
                        envios.Add(new ProductoEnvio
                        {
                            Almacen = element.Almacen,
                            Producto = element.Producto,
                            CantidadAlmacen = element.Cantidad,
                            CantidadEnvio = CantidadEnvio.ToList()[0].Cantidad,
                            Observacion = "Almacen con existencia unica del producto para un solo envio"
                        });
                        if (!almaceneUnico.Contains(element.Almacen))
                            almaceneUnico += ", " + element.Almacen;
                        contadorPrimario++;
                    }
                    if (indice == list.Count() - 1)
                        indice = 0;
                    else
                        indice++;
                }
            }
            //inicio sumatoria
            if (todosNoCompleto.Count() > 0)
            {
                foreach (var producto in cantidad)
                {
                    var sumaTotal = 0;
                    var banderaCicloUnico = true;
                    var pasosCicloUnico = 1;
                    var usadoAnteriormente = true;

                    foreach (var list in todosNoCompleto)
                    {
                    restart:
                        foreach (var element in list)
                        {
                            if (producto.Descripcion.Equals(element.Producto))
                            {
                                // almacen usados en la primera lista de unicos y otros
                                if (sumaTotal < producto.Cantidad && (almaceneUnico.Contains(element.Almacen) || almacenSecundario.Contains(element.Almacen)) && (!almaceneUnico.Equals("") || !almacenSecundario.Equals("")) && banderaCicloUnico)
                                {
                                    sumaTotal += element.Cantidad;
                                    envios.Add(new ProductoEnvio
                                    {
                                        Almacen = element.Almacen,
                                        Producto = element.Producto,
                                        CantidadAlmacen = element.Cantidad,
                                        CantidadEnvio = sumaTotal <= producto.Cantidad ? element.Cantidad : element.Cantidad - (sumaTotal - producto.Cantidad),
                                        Observacion = "Almacen con envios unicos o existencia para envio usado anteriormente con una parte del total del producto"
                                    });

                                    var cantidadAlmacen = almaceneUnico.Split(',');
                                    var cantidadAlmacenSec = almacenSecundario.Split(',');
                                    var cantidadUnion = cantidadAlmacen.Union(cantidadAlmacenSec);

                                    pasosCicloUnico++;
                                    if (sumaTotal >= producto.Cantidad)
                                    {
                                        break;
                                    }
                                    else if (element.Almacen.Equals(list[list.Count() - 1].Almacen) || cantidadUnion.Count() == pasosCicloUnico)
                                    {
                                        banderaCicloUnico = false;
                                        goto restart;
                                    }

                                }
                                else if (almaceneUnico.Equals("") && almacenSecundario.Equals("") && banderaCicloUnico)
                                {
                                    banderaCicloUnico = false;
                                    goto restart;
                                }
                                //rebisa si no se completo y manda de los otros
                                if (sumaTotal < producto.Cantidad && !almaceneUnico.Contains(element.Almacen) && !almacenSecundario.Contains(element.Almacen) && !banderaCicloUnico)
                                {
                                    //solo entra una ves cuando empieza a usar este tipo de almacen
                                    if (almacenCompuesto.Equals(""))
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
                                    else //rebisa si se manda de un almacen usado anteriormente
                                    if (almacenCompuesto.Contains(element.Almacen) && usadoAnteriormente)
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
                                    //ya si de los usados anteriormente no se completa empieza usar de los otros
                                    else if (!almacenCompuesto.Contains(element.Almacen) && !usadoAnteriormente)
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


                                    if (sumaTotal >= producto.Cantidad)
                                    {
                                        break;
                                    }
                                    else if (element.Almacen.Equals(list[list.Count() - 1].Almacen))
                                    {
                                        usadoAnteriormente = false;
                                        goto restart;
                                    }
                                }

                                if (sumaTotal < producto.Cantidad && element.Almacen.Equals(list[list.Count() - 1].Almacen) && usadoAnteriormente)
                                {
                                    usadoAnteriormente = false;
                                    goto restart;
                                }
                            }
                        }
                    }
                }
            }

            string caja = "";
            var productoAnterior = "";

            foreach (var item in envios)
            {

                if (productoAnterior.Equals(item.Producto))
                {
                    caja += string.Format(" {0}, {1};", item.Almacen, item.CantidadEnvio);
                }
                else
                {
                    caja += string.Format("\n\n {0}, {1}, {2};", item.Producto, item.Almacen, item.CantidadEnvio);
                }
                //caja += string.Format("Sucursal: {0} Contiene: {1} del Producto {2} se enviaran {3} detalle: {4}", item.Almacen, item.CantidadAlmacen, item.Producto, item.CantidadEnvio, item.Observacion);


                productoAnterior = item.Producto;
            }
            Console.WriteLine(caja);

            date = DateTime.Now.ToString("h:mm:ss:fff");
            Console.WriteLine("\n\nFin {0}", date);
            Console.ReadLine();
        }
    }
}
