namespace AIMS3.BackEnd.Modules
{
	public interface IActuator
	{
		string GetStringStatistics();
		void UploadStatistics(string data);
	}
}