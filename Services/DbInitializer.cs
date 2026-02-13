using Microsoft.EntityFrameworkCore;
using CSE325FinalProject.Data;

namespace CSE325FinalProject.Services;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context)
    {
        // Simple migration to ensure view_count column exists
        try 
        {
            // Use lowercase 'skills' and 'view_count' to match schema conventions
            try 
            {
                 // Attempt to add the column. If it exists, this will throw, which we catch.
                 // We use "IF NOT EXISTS" syntax for MySQL 8.0+ if supported, but simple ALTER is more compatible if we catch error.
                 // MySQL 8.0 supports ADD COLUMN IF NOT EXISTS? Yes.
                 context.Database.ExecuteSqlRaw("ALTER TABLE skills ADD COLUMN IF NOT EXISTS view_count INT NOT NULL DEFAULT 0;");
                 Console.WriteLine("DbInitializer: Successfully ensured view_count column exists.");
            }
            catch (Exception ex)
            {
                // Fallback for older MySQL versions or other errors
                Console.WriteLine($"DbInitializer: Error adding column (might already exist): {ex.Message}");
                
                // Try without IF NOT EXISTS if syntax error
                 try 
                {
                     context.Database.ExecuteSqlRaw("ALTER TABLE skills ADD COLUMN view_count INT NOT NULL DEFAULT 0;");
                     Console.WriteLine("DbInitializer: Added view_count column (fallback).");
                }
                catch (Exception ex2)
                {
                     Console.WriteLine($"DbInitializer: Fallback failed: {ex2.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DbInitializer: Critical error: {ex.Message}");
        }
    }
}
