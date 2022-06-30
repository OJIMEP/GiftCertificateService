namespace GiftCertificateService.Data
{
    public static class Queries
    {
        public static string CertInfo = @"SELECT
	[_IDRRef],
	[_Fld4242] 
Into #Temp_CertRef
FROM
	[triovist].[dbo].[_Reference172] 
where
	[_Fld4242] IN(@Barcode);

Select
	T1._Fld14496RRef AS CertRef,
	T1._Fld14497RRef AS CertStatus 
Into #Temp_CertStatus
From
	[triovist].[dbo].[_InfoRg14495] T1 --cert history
	Inner join (
		SELECT
			Max([_Period]) AS [_Period],
			[_Fld14496RRef]
		FROM
			[triovist].[dbo].[_InfoRg14495]
			Inner Join #Temp_CertRef On _Fld14496RRef = [_IDRRef] 
		Where
			[_Active] = 0x01
			And [_Period] <= Dateadd(Year, 2000, GETDATE())
		group by
			[_Fld14496RRef]
			) T2 On T1._Fld14496RRef = T2._Fld14496RRef
			And T1.[_Period] = T2.[_Period]
		Where
			_Fld14497RRef IN (--filter by status
				0xB3A1D155BEA215A74F77177CEA264869, 
				0x8EABEBCCF5A9FBF74BCB7DA9464028AE
			);

SELECT
	[_Fld4242] AS Barcode,
	Sum([_Fld16861]) AS SumLeft
FROM
	#Temp_CertRef
	Inner join [triovist].[dbo].[_AccumRgT16863] SumRemains
		On SumRemains.[_Fld16860RRef] = #Temp_CertRef.[_IDRRef]
	Inner join #Temp_CertStatus 
		On SumRemains.[_Fld16860RRef] = #Temp_CertStatus.CertRef
Where
	[_Period] = '5999-11-01T00:00:00'
Group by
	[_Fld16860RRef],
	[_Fld4242],
	CertStatus
Having Sum([_Fld16861]) > 0;";

		public const string DatebaseBalancingReplicaFull = @"select datediff(ms, last_commit_time, getdate())
from [master].[sys].[dm_hadr_database_replica_states]";

		public const string DatebaseBalancingMain = @"select top (1) _IDRRef from dbo._Reference112";

		public const string DatebaseBalancingReplicaTables = @"Select TOP(1) _IDRRef FROM dbo._Reference99";
	}
}
