using Domain;

namespace DomainTest
{
    public class CowTests
    {
        private Cow OutCow {  get; set; }
        [SetUp]
        public void Setup()
        {
            OutCow = new Cow();
        }

        [Test]
        public void Cow_Exist()
        {

            Assert.That(OutCow is not null);
        }

        [Test]
        public void CanSetMilk()
        {
            Assert.That(OutCow.Milk is null);
            Milk m = new Milk();
            OutCow.SetMilk(m);
            Assert.That(OutCow.Milk, Is.EqualTo(m));
        }
        [Test]
        public void CanGetMilkLiters()
        {
            Assert.That(OutCow.Milk is null);
            Milk m = new Milk();
            m.Litters = 100;
            OutCow.SetMilk(m);
            Assert.That(OutCow.GetMilkQuantity(), Is.EqualTo(100));
        }
    }
}
