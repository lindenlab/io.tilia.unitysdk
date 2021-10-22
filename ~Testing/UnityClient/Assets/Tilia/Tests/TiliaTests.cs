using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using UnityEngine.TestTools;
using System.Threading;
using System.IO;

namespace Tilia.Tests
{
    public class TiliaTests
    {
        private GameObject TestObject;
        private TiliaPay TiliaPaySDK;

        private string UserID;
        private string InvoiceID;
        private string EscrowID;
        private string PaymentID;

        private string TestClientID;
        private string TestClientSecret;

        [UnitySetUp]
        public IEnumerator Init()
        {
            if (string.IsNullOrEmpty(TestClientID))
            {
                string GUIDPath = UnityEditor.AssetDatabase.GUIDToAssetPath("7d6bdcf9af7838e4788b8f09efc8ae43").Substring(7);
                string filePath = Path.Combine(Application.dataPath, GUIDPath);
                string[] fileData = File.ReadAllText(filePath).Split("\r"[0]);
                TestClientID = fileData[0].Replace("\n", "").Replace("\r", "");
            }

            if (string.IsNullOrEmpty(TestClientSecret))
            {
                string GUIDPath = UnityEditor.AssetDatabase.GUIDToAssetPath("925df04337594c6478891dc2540b44af").Substring(7);
                string filePath = Path.Combine(Application.dataPath, GUIDPath);
                string[] fileData = File.ReadAllText(filePath).Split("\r"[0]);
                TestClientSecret = fileData[0].Replace("\n", "").Replace("\r", "");
            }

            if (TestObject == null)
            {
                TestObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            }

            if (TiliaPaySDK == null)
            {
                TiliaPaySDK = TestObject.AddComponent<TiliaPay>();
                TiliaPaySDK.StagingClientID = TestClientID;
                TiliaPaySDK.StagingClientSecret = TestClientSecret;
                TiliaPaySDK.StagingEnvironment = true;
                TiliaPaySDK.LoggingEnabled = true;
            }

            yield return new EnterPlayMode();
        }

        [UnityTest, Order(1)]
        public IEnumerator TestUserCreation()
        {
            var userName = System.Guid.NewGuid().ToString() + "@tilia-sdk.test";
            var password = "T3St%SdK_Unity";
            var newUser = new TiliaNewUser()
            {
                UserName = userName,
                Password = password,
                Email = userName
            };

            bool finished = false;
            var startTime = Time.time;

            TiliaPaySDK.RegisterUser(newUser,
                (value) =>
                {
                    if (value.Success)
                    {
                        if (!string.IsNullOrEmpty(value.AccountID))
                            UserID = value.AccountID;

                        finished = true;
                    }
                });


            while (!finished && (Time.time - startTime) < 5.0)
            {
                yield return 0.1;
            }

            Assert.IsNotNull(UserID);
            yield return null;
        }

        [UnityTest, Order(2)]
        public IEnumerator TestKYC()
        {
            if (string.IsNullOrEmpty(UserID))
            {
                Assert.IsNotNull(UserID);
                yield return null;
            }

            bool finished = false;
            var startTime = Time.time;
            string state = null;

            TiliaPaySDK.CheckKYC(UserID,
                (value) =>
                {
                    if (value.Success)
                    {
                        if (!string.IsNullOrEmpty(value.State))
                            state = value.State;

                        finished = true;
                    }
                });


            while (!finished && (Time.time - startTime) < 5.0)
            {
                yield return 0.1;
            }

            Assert.IsNotNull(state);

            yield return null;
        }

        [UnityTest, Order(3)]
        public IEnumerator TestUserInfo()
        {
            if (string.IsNullOrEmpty(UserID))
            {
                Assert.IsNotNull(UserID);
                yield return null;
            }

            bool finished = false;
            var startTime = Time.time;
            string id = null;

            TiliaPaySDK.GetUserInfo(UserID,
                (value) =>
                {
                    if (value.Success)
                    {
                        if (!string.IsNullOrEmpty(value.ID))
                            id = value.ID;

                        finished = true;
                    }
                });


            while (!finished && (Time.time - startTime) < 5.0)
            {
                yield return 0.1;
            }

            Assert.IsNotNull(id);

            yield return null;
        }

        [UnityTest, Order(4)]
        public IEnumerator TestPaymentMethods()
        {
            if (string.IsNullOrEmpty(UserID))
            {
                Assert.IsNotNull(UserID);
                yield return null;
            }

            bool finished = false;
            var startTime = Time.time;

            TiliaPaySDK.GetPaymentMethods(UserID,
                (value) =>
                {
                    if (value.Success)
                    {
                        if (value.PaymentMethods.Count > 0)
                        {
                            PaymentID = value.PaymentMethods[0].ID;
                        }
                        finished = true;
                    }
                });


            while (!finished && (Time.time - startTime) < 5.0)
            {
                yield return 0.1;
            }

            Assert.IsTrue(finished);

            yield return null;
        }

        [UnityTest, Order(5)]
        public IEnumerator TestInvoiceCreation()
        {
            if (string.IsNullOrEmpty(UserID))
            {
                Assert.IsNotNull(UserID);
                yield return null;
            }
            else if (string.IsNullOrEmpty(PaymentID))
            {
                Assert.IsNotNull(PaymentID);
                yield return null;
            }

            bool finished = false;
            var startTime = Time.time;

            int price = 0;
            string product = "tilia_sdk_unity_test_product";
            var newItem = new TiliaLineItem()
            {
                ProductSKU = product,
                Description = "A cool product you really need.",
                Amount = price,
                Currency = "USD",
                TransactionType = "user_to_integrator",
                ReferenceType = "integrator_product_id",
                ReferenceID = product
            };
            var newPurchase = new TiliaNewInvoice()
            {
                AccountID = UserID,
                ReferenceType = "product_purchase_id",
                ReferenceID = "invoice_" + product,
                Description = "A great purchase full of cool stuff."
            };
            newPurchase.LineItems.Add(newItem);
            var newPayment = new TiliaPayment()
            {
                Amount = 0,
                ID = PaymentID
            };
            newPurchase.PaymentMethods.Add(newPayment);
            TiliaPaySDK.CreateInvoice(newPurchase,
                (value) =>
                {
                    if (value.Success)
                    {
                        if (!string.IsNullOrEmpty(value.ID))
                            InvoiceID = value.ID;
                    }

                    finished = true;
                });

            while (!finished && (Time.time - startTime) < 5.0)
            {
                yield return 0.1;
            }

            Assert.IsNotNull(InvoiceID);

            yield return null;
        }

        [UnityTest, Order(6)]
        public IEnumerator TestInvoicePayment()
        {
            if (string.IsNullOrEmpty(UserID))
            {
                Assert.IsNotNull(UserID);
                yield return null;
            }
            else if (string.IsNullOrEmpty(InvoiceID))
            {
                Assert.IsNotNull(InvoiceID);
                yield return null;
            }

            bool finished = false;
            var startTime = Time.time;

            TiliaPaySDK.PayInvoice(InvoiceID,
                (value) =>
                {
                    if (value.Success)
                    {
                        finished = true;
                    }
                });


            while (!finished && (Time.time - startTime) < 5.0)
            {
                yield return 0.1;
            }

            Assert.IsTrue(finished);

            yield return null;
        }

        [UnityTest, Order(7)]
        public IEnumerator TestClientRedirect()
        {
            if (string.IsNullOrEmpty(UserID))
            {
                Assert.IsNotNull(UserID);
                yield return null;
            }

            bool finished = false;
            var startTime = Time.time;
            string redirect = null;

            TiliaPaySDK.RequestClientRedirectURL(UserID, new string[] { "user_info" },
                (value) =>
                {
                    if (value.Success)
                    {
                        if (!string.IsNullOrEmpty(value.Redirect))
                            redirect = value.Redirect;

                        finished = true;
                    }
                });


            while (!finished && (Time.time - startTime) < 5.0)
            {
                yield return 0.1;
            }

            Assert.IsNotNull(redirect);

            yield return null;
        }

        [UnityTest, Order(8)]
        public IEnumerator TestCreateEscrow()
        {
            if (string.IsNullOrEmpty(UserID))
            {
                Assert.IsNotNull(UserID);
                yield return null;
            }
            else if (string.IsNullOrEmpty(PaymentID))
            {
                Assert.IsNotNull(PaymentID);
                yield return null;
            }

            bool finished = false;
            var startTime = Time.time;

            int price = 0;
            string product = "tilia_sdk_unity_test_product";
            var newItem = new TiliaLineItem()
            {
                ProductSKU = product,
                Description = "A cool product you really need.",
                Amount = price,
                Currency = "USD",
                TransactionType = "user_to_integrator",
                ReferenceType = "integrator_product_id",
                ReferenceID = product
            };
            var newPurchase = new TiliaNewInvoice()
            {
                AccountID = UserID,
                ReferenceType = "product_purchase_id",
                ReferenceID = "escrow_" + product,
                Description = "A great purchase full of cool stuff."
            };
            newPurchase.LineItems.Add(newItem);
            var newPayment = new TiliaPayment()
            {
                Amount = 0,
                ID = PaymentID
            };
            newPurchase.PaymentMethods.Add(newPayment);
            TiliaPaySDK.CreateEscrow(newPurchase,
                (value) =>
                {
                    if (value.Success)
                    {
                        if (!string.IsNullOrEmpty(value.ID))
                            EscrowID = value.ID;
                    }

                    finished = true;
                });

            while (!finished && (Time.time - startTime) < 5.0)
            {
                yield return 0.1;
            }

            Assert.IsNotNull(EscrowID);

            yield return null;
        }

        [UnityTest, Order(9)]
        public IEnumerator TestPayEscrow()
        {
            if (string.IsNullOrEmpty(UserID))
            {
                Assert.IsNotNull(UserID);
                yield return null;
            }
            else if (string.IsNullOrEmpty(EscrowID))
            {
                Assert.IsNotNull(EscrowID);
                yield return null;
            }

            bool finished = false;
            var startTime = Time.time;

            TiliaPaySDK.PayEscrow(EscrowID,
                (value) =>
                {
                    if (value.Success)
                    {
                        finished = true;
                    }
                });


            while (!finished && (Time.time - startTime) < 5.0)
            {
                yield return 0.1;
            }

            Assert.IsTrue(finished);

            yield return null;
        }

        [UnityTest, Order(10)]
        public IEnumerator TestCommitEscrow()
        {
            if (string.IsNullOrEmpty(UserID))
            {
                Assert.IsNotNull(UserID);
                yield return null;
            }
            else if (string.IsNullOrEmpty(EscrowID))
            {
                Assert.IsNotNull(EscrowID);
                yield return null;
            }

            bool finished = false;
            var startTime = Time.time;

            TiliaPaySDK.CommitEscrow(EscrowID,
                (value) =>
                {
                    if (value.Success)
                    {
                        finished = true;
                    }
                });


            while (!finished && (Time.time - startTime) < 5.0)
            {
                yield return 0.1;
            }

            Assert.IsTrue(finished);

            yield return null;
        }
    }
}
