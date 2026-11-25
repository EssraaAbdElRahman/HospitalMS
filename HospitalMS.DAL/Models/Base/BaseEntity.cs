namespace HospitalMS.DAL.Models.Base
{
    public abstract class BaseEntity
    {
        public Guid Id { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public string? CreatedBy { get; private set; }

        public DateTime? UpdatedAt { get; private set; }
        public string? UpdatedBy { get; private set; }

        public DateTime? DeletedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public string? DeletedBy { get; private set; }

        protected BaseEntity()
        {
            Id = Guid.NewGuid();
        }
        public void Created(string createdBy)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);


            CreatedBy = createdBy;
            CreatedAt = DateTime.UtcNow;
        }
        public void Updated(string updatedBy)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

            UpdatedBy = updatedBy;
            UpdatedAt = DateTime.Now;
        }

        public void Deleted(string deletedBy)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);

            if (IsDeleted)
                throw new InvalidOperationException("Entity is already deleted.");

            IsDeleted = true;
            DeletedBy = deletedBy;
            DeletedAt = DateTime.UtcNow;
        }
    }
}
