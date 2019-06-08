using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using PaparaMVCExample.Models;

namespace PaparaMVCExample.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        #region // CLASSLAR

        public static string paparaApiKey = "AJHSDFGQUWYGDOAHSDBASDBKUYQWGDKA";
        public static string merchantSecretKey = "SDFDFGHFGHERTSDFSDFSEDRFWESFD";
       

        public class PaparaRedirect
        {
            public string paymentId { get; set; }
            public string referenceId { get; set; }
            public int status { get; set; }
            public double amount { get; set; }
        }
        public class Payment
        {
            public double amount { get; set; }
            public string referenceId { get; set; }
            public string orderDescription { get; set; }
            public string notificationUrl { get; set; }
            public string redirectUrl { get; set; }
        }
        public class PaymentData
        {
            public string merchantId { get; set; }
            public string userId { get; set; }
            public int paymentMethod { get; set; }//0-Kullanıcı işlemi var olan Papara bakiyesi ile tamamladı //1-Kullanıcı işlemi hesabına daha önce tanımladığı banka/kredi kartı ile tamamladı //2-Kullanıcı işlemi mobil ödeme ile tamamladı 
            public string paymentMethodDescription { get; set; }
            public string referenceId { get; set; }
            public string orderDescription { get; set; }
            public int status { get; set; }//0-Beklemede, ödeme henüz yapılmadı //1-Ödeme yapıldı, işlem tamamlandı //2-İşlem üye işyeri tarafından iade edildi. 
            public string statusDescription { get; set; }
            public double amount { get; set; }
            public string currency { get; set; }
            public string notificationUrl { get; set; }
            public bool notificationDone { get; set; }
            public string redirectUrl { get; set; }
            public string merchantSecretKey { get; set; }
            public string paymentUrl { get; set; }
            public string returningRedirectUrl { get; set; }
            public string id { get; set; }
            public string createdAt { get; set; }
        }
        public class PaymentError
        {
            public string message { get; set; }
            public int code { get; set; }
        }
        public class PaymentGet
        {
            public PaymentData data { get; set; }
            public bool succeeded { get; set; }
            public PaymentError error { get; set; }
        }

        /* 
         * CreatePaymentObject Ödeme nesnesi oluşturmak için kullanılır.
         * Papara sunucularında yeni bir payment oluşturur ve geri döndürür.
         * Her sepet sonucuna gidildiğinde oluşturulmalıdır. /Home/Checkout/ sayfası gibi. 
         * Bir Payment Get nesnesi döndürür
         * 
         */
        public static PaymentGet CreatePaymentObject(Payment payment)
        {
            PaymentGet newPayment = new PaymentGet();
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://merchantapi-test-master.papara.com/payments");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                httpWebRequest.Headers.Add("ApiKey", paparaApiKey);
                
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = JsonConvert.SerializeObject(payment,Formatting.Indented);
                    streamWriter.Write(json);
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var responseText = streamReader.ReadToEnd();
                    
                    newPayment = JsonConvert.DeserializeObject<PaymentGet>(responseText);
                    return newPayment;
                }
            }
            catch (Exception e)
            {
                newPayment.data = new PaymentData
                {
                    amount = 0,
                    createdAt = "",
                    currency = "",
                    id = "",
                    merchantId = "",
                    merchantSecretKey = "",
                    notificationDone = false,
                    notificationUrl = "",
                    orderDescription = "",
                    paymentMethod = 0,
                    paymentMethodDescription = "",
                    paymentUrl = "",
                    redirectUrl = "",
                    referenceId = "",
                    returningRedirectUrl = "",
                    status = 0,
                    statusDescription = "",
                    userId = ""
                };
                newPayment.succeeded = false;
                newPayment.error = new PaymentError
                {
                    message = e.Message,
                    code = 401
                };
                return newPayment;
            }
        }

        /*
         * GetPaymentObject, Ödeme nesnesi getirmek için kullanılır. 
         * Papara sunucularından id (ödeme numarası) parametresiyle istenerek getirilir.
         * Bir PaymentGet nesnesi döndürür. 
         * İçerisinde ödeme verisi, başarı durumu ve var ise hata açıklaması bulunur.
        */
        public static PaymentGet GetPaymentObject(string id)
        {
            PaymentGet paymentGet = new PaymentGet();
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://merchantapi-test-master.papara.com/payments?id=" + id);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "GET";
                httpWebRequest.Headers.Add("ApiKey", paparaApiKey);
                httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                

                //JavaScriptSerializer js = new JavaScriptSerializer();
                using (HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    var responseText = reader.ReadToEnd();
                    paymentGet = JsonConvert.DeserializeObject<PaymentGet>(responseText);
                    return paymentGet;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                paymentGet.data = new PaymentData { id = id };
                paymentGet.error = new PaymentError { code = 0, message = e.Message };
                paymentGet.succeeded = false;
                return paymentGet;
            }
        }

        /*
         * RefundPayment ödeme iptali durumlarında kullanılır. 
         * Ödemede hata olduğunda (az ödeme, çok ödeme, sahte ödeme ihtimalinde) veya sipariş iptali sayfalarında kullanılmalıdır.
         * 
        */
        public static bool RefundPayment(string id)
        {
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://merchantapi-test-master.papara.com/payments?id=" + id);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "PUT";
                httpWebRequest.Headers.Add("ApiKey", paparaApiKey);
                httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                
                using (HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    var responseText = reader.ReadToEnd();
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
        #endregion

        #region // SAYFA ÇAĞRIMLARI


        //Checkout sayfasından sonra 'satın al' butonuyla çağrılacak metotdur.
        public IActionResult Buy()
        {
            try
            {
                //Sepet oluşturulmamış, sepete geri dön.
                if (HttpContext.Session.GetString("cart") == null)
                {
                    return RedirectToAction("Cart", "Home");
                }
                //Sepet var
                else
                {
                    //Papara için ödeme alt yapısı oluşturulur
                    Payment payment;

                    //FATURA OLUŞTURULDU
                    try
                    {

                        //Invoice newInvoice = new Invoice();
                        //BUrada newInvoice içerisi doldurulur. İçine sepetteki ürünler eklenir en son subtotal hesaplanır.

                        double totalAmount = 1234.56;

                        payment = new Payment()
                        {
                            amount = totalAmount,
                            notificationUrl = "http://" + Request.Host.Host + ':' + Request.Host.Port + "/Home/BuyNotification",
                            orderDescription = "Satın alınan şeylerin listesi",
                            redirectUrl = "http://" + Request.Host.Host + ':' + Request.Host.Port + "/Home/BuyRedirect",
                            referenceId = "invoiceId_" + "987654"
                        };
                    }
                    //FATURA OLUŞTURULAMADI
                    catch (Exception e)
                    {
                        ViewBag.message = e.Message;
                        return View("Error404");
                    }
                    //FATURA OLUŞTURULDUĞU VARSAYILDIĞINDA ÖDEME OLUŞTURULABİLECEK Mİ
                    try
                    {
                        PaymentGet paymentResponse = CreatePaymentObject(payment);

                        //FATURA OLUŞTURULDU VE ÖDEME İSTEĞİ OLUŞTURULDU, SEPETTEKİ ÜRÜNLER FATURAYA AİT FATURAÜRÜNLERİ TABLONUZA AKTARILABİLİR VE ÖDEMEYE YÖNLENDİRİLEBİLİR
                        if (paymentResponse.succeeded == true)
                        {
                            
                            /*
                                
                            */
                            return Redirect(paymentResponse.data.paymentUrl);
                        }
                        else
                        {//FATURA OLUŞTURULDU ANCAK ÖDEME İSTEĞİ OLUŞTURULAMADI İSE
                            
                            //OLUŞTURULAN FATURAYI BURADA VERİTABANINDAN SİLEBİLİRSİNİZ

                            ViewBag.message = "Papara'da invoice oluştururken bir hata oluştu | " + paymentResponse.error.message;
                            return View("Error404");
                        }
                    }
                    catch (Exception e)
                    {//FATURA OLUŞTURULDUĞU VARSAYILDI ANCA ÖDEME OLUŞTURULAMADI
                        ViewBag.message = e.Message;
                        //return RedirectToAction("Lisanslar","SirketYonetim",new PaymentError {code=0, message=e.Message});
                        return View("Error404");
                    }


                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                ViewBag.message = e.Message;
                return View("Error404");
            }
        }


        /*
         * Buy() Methodunda Papara'ya gönderilen sonuç döndürme linkine ait methoddur..
         * 
         * 
        */
        [HttpGet]
        public IActionResult BuyRedirect(string paymentId, int status, double amount)
        {
            PaymentGet payment = GetPaymentObject(paymentId); //Papara sunucusundan ödeme bilgisini getir.
            
            //ÖDEME BAŞARILI
            if (payment.succeeded == true)
            {
                int invoiceId = Convert.ToInt32(payment.data.referenceId.Split('_')[1]);

                //invoiceId ile faturaya gidilir ve faturada yazan miktar totalAmount'a yazılır.

                double totalAmount = 1234.56;

                //Sitemiz üzerinde çıkan tutarla eş ödeme yapılmıştır. Fatura kontrolüne geçebiliriz.
                if (payment.data.amount == totalAmount)
                {
                    
                    //Gelen ödeme bildiriminde bahsi geçen invoiceId'si veritabanımızda bulunmamakta
                    if (false /*dbModel.Invoice.Where(x => x.invoiceId == invoiceId).Count()==0*/)
                    {
                        //Sitemizi ilgilendirmeyen kısım.
                    }
                    //Gelen ödeme bildiriminde bahsi geçen invoiceId'si veritabanımızda bulunuyor.
                    else
                    {

                        /*
                            Invoice invoice = dbModel.Invoice.Where(x => x.invoiceId == invoiceId).First();
                        */

                        //Veritabanımızdaki eşleşmeyle bulduğumuz invoice'nin henüz ödenmemiş olduğunu varsayalım.
                        //Yukarıda Invoice tipinde bir invoice değişkeni tanımlıyorsanız, demo açısından dynamic invoice yapmamıza gerek yoktur.
                        dynamic invoice = new { isPaid = false }; 


                        //Gelen ödeme bildiriminde bahsi geçen invoice zaten ödenmiştir. 
                        if (invoice.isPaid == true)
                        {
                            
                            HttpContext.Session.SetString("cart",null);

                            ViewBag.message = "Faturanız ödenmiştir. Yönetim panelinden satın almış olduğunuz ürünlerle ilgili işlem yapabilirsiniz.";
                        }
                        //Gelen ödeme bildiriminde bahsi geçen invoice henüz ödenmemiştir. Veritabanında ödendi olarak gösterilmeli ve ürünler tayin edilmeli
                        else
                        {
                            
                            
                            HttpContext.Session.SetString("cart", null);

                            ViewBag.message = "Faturanız ödenmiştir. Yönetim panelinden satın almış olduğunuz lisanslarla ilgili işlem yapabilirsiniz.";
                        }
                    }
                }
                //Sitemiz üzerinde çıkan tutardan fazla ödeme yapılmıştır. Ödenmiş olan ücret geri iade edilecek. Ve satın almış olduğunuz ürünler iptal edilecek.
                else if (payment.data.amount > totalAmount)
                {
                    ViewBag.message = "Sitemiz üzerinde çıkan tutardan fazla ödeme yapılmıştır. Ödenmiş olan ücret geri iade edilecek";
                    bool refundStatus = RefundPayment(payment.data.id);

                    //EĞER GERİ ÖDEME BAŞARILIYSA
                    if (refundStatus == true)
                    {
                        
                        ViewBag.message = "Sitemiz üzerinde çıkan tutardan fazla ödeme yapılmıştır. Sepetiniz iptal edildi. Ödenmiş olan ücret PAPARA'ya geri iade edildi.";
                        HttpContext.Session.SetString("cart", null);
                    }
                    //EĞER GERİ ÖDEME BAŞARISIZSA
                    else
                    {
                        ViewBag.message = "Sitemiz üzerinde çıkan tutardan fazla ödeme yapılmıştır. Sepetiniz iptal edildi. Ve bir hatadan ötürü ödenmiş olan ücret PAPARA'ya geri iade edilemedi. Lütfen bizimle irtibata geçin.";
                        HttpContext.Session.SetString("cart", null);
                    }
                }
                //Sitemiz üzerinde çıkan tutardan az ödeme yapılmıştır. Ödenmiş olan ücret geri iade edilecek. Ve satın almış olduğunuz ürünler iptal edilecek.
                else
                {
                    
                    ViewBag.message = "Sitemiz üzerinde çıkan tutardan az ödeme yapılmıştır. Ücret geri iade edilecek.";
                    bool refundStatus = RefundPayment(payment.data.id);
                    //EĞER GERİ ÖDEME BAŞARILIYSA
                    if (refundStatus == true)
                    {
                        ViewBag.message = "Sitemiz üzerinde çıkan tutardan az ödeme yapılmıştır. Sepetiniz iptal edildi. Ve ödenmiş olan ücret PAPARA'ya geri iade edildi. Yönetim panelinizden ödenmemiş faturalarınıza göz atıp";
                        HttpContext.Session.SetString("cart", null);
                    }
                    //EĞER GERİ ÖDEME BAŞARISIZSA
                    else
                    {
                        
                        ViewBag.message = "Sitemiz üzerinde çıkan tutardan az ödeme yapılmıştır. Sepetiniz iptal edildi. Ve bir hatadan ötürü ödenmiş olan ücret PAPARA'ya geri iade edilemedi. Lütfen bizimle irtibata geçin.";
                        HttpContext.Session.SetString("cart", null);
                    }
                }

            }
            //ÖDEME BAŞARISIZ
            else
            {

                int invoiceId = Convert.ToInt32(payment.data.referenceId.Split('_')[1]);
                try
                {
                    ViewBag.message = "Ödeme başarısız. Faturanız ve ona bağlı olan tüm ürünler veritabanımızdan silindi. Lütfen sepetinizi yeniden oluşturup ödeme yapınız.";
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            }
            return View("CheckoutFinished");
        }

        /*
         * 
         * Papara, HttpStatusCode.OK döndürene kadar BuyNotification'a 24 saat boyunca çağrı yapmaya devam eder.
         * 
        */
        [HttpPost]
        public HttpStatusCode BuyNotification(PaymentData paymentData)
        {
            //PAPARA, ÖDENMEMİŞ / TAMAMLANMAMIŞ ÖDEMELER İÇİN BİLDİRİMDE BULUNMAZ

            int invoiceId = Convert.ToInt32(paymentData.referenceId.Split('_')[1]);


            double subTotal = 0;
            double taxPercent = 18;
            double amount = (subTotal * taxPercent / 100 + subTotal);

            //Sitemiz üzerinde çıkan tutarla eş ödeme yapılmıştır. Fatura kontrolüne geçebiliriz.
            if (paymentData.amount == (subTotal * taxPercent / 100 + subTotal) )
            {
                //Gelen ödeme bildiriminde bahsi geçen invoiceId'si veritabanımızda bulunmamakta
                if ( false/*dbModel.Invoice.Where(x => x.invoiceId == invoiceId).Count==0*/)
                {
                    

                    bool refundStatus = RefundPayment(paymentData.id);
                    //EĞER GERİ ÖDEME BAŞARILIYSA
                    if (refundStatus == true)
                    {
                        ViewBag.message = "Ödeme yaptığınız invoice sistemimizde bulunmamaktadır. Sepetiniz iptal edildi. Ödenmiş olduğunuz ücret PAPARA'ya geri iade edildi. Ücretin size iade süresi PAPARA'nın günlük durumuna göre değişebilir.";
                        HttpContext.Session.SetString("cart", null);
                    }
                    //EĞER GERİ ÖDEME BAŞARISIZSA
                    else
                    {
                        ViewBag.message = "Sitemiz üzerinde çıkan tutardan fazla ödeme yapılmıştır. Sepetiniz iptal edildi. Ve sistemde bulunan bir hatadan ötürü ödenmiş olan ücret PAPARA'ya geri iade edilemedi. Lütfen bizimle irtibata geçin.";
                        HttpContext.Session.SetString("cart", null);
                    }
                    return HttpStatusCode.OK;
                }
                //Gelen ödeme bildiriminde bahsi geçen invoiceId'si veritabanımızda bulunuyor.
                else
                {
                    //Invoice invoice = dbModel.Invoice.Where(x => x.invoiceId == invoiceId).First();
                    //Veritabanımızdaki eşleşmeyle bulduğumuz invoice'nin henüz ödenmemiş olduğunu varsayalım.
                    //Yukarıda Invoice tipinde bir invoice değişkeni tanımlıyorsanız, demo açısından dynamic invoice yapmamıza gerek yoktur.
                    dynamic invoice = new { isPaid = false };

                    //Gelen ödeme bildiriminde bahsi geçen invoice zaten ödenmiştir
                    if (invoice.isPaid == true)
                    {
                        ViewBag.message = "Fatura zaten ödenmiştir. Son yaptığınız ödeme geri ödenecektir.";

                        bool refundStatus = RefundPayment(paymentData.id);
                        //EĞER GERİ ÖDEME BAŞARILIYSA
                        if (refundStatus == true)
                        {
                            
                            ViewBag.message = "Fatura zaten ödenmiştir. Son yaptığınız ödeme geri ödenecektir. Sepetiniz iptal edildi. Ödenmiş olan ücret PAPARA'ya geri iade edildi.";
                            HttpContext.Session.SetString("cart", null);
                        }
                        //EĞER GERİ ÖDEME BAŞARISIZSA
                        else
                        {

                            ViewBag.message = "Sitemiz üzerinde çıkan tutardan fazla ödeme yapılmıştır. Sepetiniz iptal edildi. Ve bir hatadan ötürü ödenmiş olan ücret PAPARA'ya geri iade edilemedi. Lütfen bizimle irtibata geçin.";
                            HttpContext.Session.SetString("cart", null);
                        }
                        return HttpStatusCode.OK;
                    }
                    //Gelen ödeme bildiriminde bahsi geçen invoice henüz ödenmemiştir. Veritabanında ödendi olarak gösterilmeli ve ürünler tayin edilmeli
                    else
                    {
                        //invoice.isPaid = true;
                        //dbModel.SaveChanges();
                        return HttpStatusCode.OK;
                    }
                }
            }
            //Sitemiz üzerinde çıkan tutardan fazla ödeme yapılmıştır. Ödenmiş olan ücret geri iade edilecek
            else if (paymentData.amount > amount )
            {
                ViewBag.message = "Sitemiz üzerinde çıkan tutardan fazla ödeme yapılmıştır. Ödenmiş olan ücret geri iade edilecek";
                bool refundStatus = RefundPayment(paymentData.id);
                //EĞER GERİ ÖDEME BAŞARILIYSA
                if (refundStatus == true)
                {
                    ViewBag.message = "Sitemiz üzerinde çıkan tutardan fazla ödeme yapılmıştır. Sepetiniz iptal edildi. Ödenmiş olan ücret PAPARA'ya geri iade edildi.";
                    HttpContext.Session.SetString("cart", null);
                }
                //EĞER GERİ ÖDEME BAŞARISIZSA
                else
                {
                    ViewBag.message = "Sitemiz üzerinde çıkan tutardan fazla ödeme yapılmıştır. Sepetiniz iptal edildi. Ve bir hatadan ötürü ödenmiş olan ücret PAPARA'ya geri iade edilemedi. Lütfen bizimle irtibata geçin.";
                    HttpContext.Session.SetString("cart", null);
                }
                return HttpStatusCode.OK;
            }
            //Sitemiz üzerinde çıkan tutardan az ödeme yapılmıştır. Ödenmiş olan ücret geri iade edilecek.
            else
            {
                ViewBag.message = "Sitemiz üzerinde çıkan tutardan az ödeme yapılmıştır. Ücret geri iade edilecek.";
                bool refundStatus = RefundPayment(paymentData.id);
                //EĞER GERİ ÖDEME BAŞARILIYSA
                if (refundStatus == true)
                {
                    ViewBag.message = "Sitemiz üzerinde çıkan tutardan az ödeme yapılmıştır. Sepetiniz iptal edildi. Ve ödenmiş olan ücret PAPARA'ya geri iade edildi. Yönetim panelinizden ödenmemiş faturalarınıza göz atın.";
                    TempData["errorMessaga"] = ViewBag.message;
                    HttpContext.Session.SetString("cart", null);
                }
                //EĞER GERİ ÖDEME BAŞARISIZSA
                else
                {
                    ViewBag.message = "Sitemiz üzerinde çıkan tutardan az ödeme yapılmıştır. Sepetiniz iptal edildi. Ve bir hatadan ötürü ödenmiş olan ücret PAPARA'ya geri iade edilemedi. Lütfen bizimle irtibata geçin.";
                    HttpContext.Session.SetString("cart", null);
                }
                return HttpStatusCode.OK;
            }
        }
        
        #endregion


    }
}